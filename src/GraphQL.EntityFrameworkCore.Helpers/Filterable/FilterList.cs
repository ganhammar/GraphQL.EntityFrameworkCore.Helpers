using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
using GraphQL.Language.AST;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FilterList
    {
        public static IQueryable<TQuery> Filter<TQuery>(this IQueryable<TQuery> query, IResolveFieldContext<object> context, IModel model)
        {
            var filter = context?.GetArgument<string>("filter");
            
            if (string.IsNullOrEmpty(filter))
            {
                return query;
            }

            return AddWhere(query, filter, context, model);
        }

        private static IQueryable<TQuery> AddWhere<TQuery>(this IQueryable<TQuery> query, string filter, IResolveFieldContext<object> context, IModel model)
        {
            var entityType = typeof(TQuery);
            var argument = Expression.Parameter(entityType);
            var expressions = GetSelectionPaths(argument, filter, entityType, context.SubFields, model, query);

            if (expressions == default)
            {
                return query;
            }

            Expression clause = default;

            expressions.ForEach(x =>
            {
                clause = clause == default ? x : Expression.Or(clause, x);
            });

            clause = Expression.Lambda(clause, argument);
            var method = GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType);

            return (IQueryable<TQuery>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static List<Expression> GetSelectionPaths<TQuery>(Expression argument, string filter, Type entityType, IDictionary<string, Field> selection, IModel model, IQueryable<TQuery> query)
        {
            var result = new List<Expression>();
            var entity = model.FindEntityType(entityType);
            var navigationProperties = entity.GetNavigations();

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var property = entityType.GetProperty(field.Value.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    var navigationProperty = navigationProperties.Where(x => x.Name == property.Name).FirstOrDefault();

                    if (navigationProperty != default)
                    {
                        var targetType = property.PropertyType;

                        if (typeof(IEnumerable).IsAssignableFrom(targetType))
                        {
                            targetType = targetType.GetGenericArguments().First();
                            var subArgument = Expression.Parameter(targetType);

                            GetSelectionPaths(
                                subArgument,
                                filter,
                                targetType,
                                ToDictionary(field),
                                model,
                                query).ForEach(x =>
                                {
                                    var anyMethod = GetAnyMethod().MakeGenericMethod(targetType);

                                    result.Add(Expression.Call(anyMethod, Expression.MakeMemberAccess(argument, property), Expression.Lambda(x, subArgument)));
                                });
                        }
                        else
                        {
                            result.AddRange(GetSelectionPaths(
                                Expression.Property(argument, property.Name),
                                filter,
                                targetType,
                                ToDictionary(field),
                                model,
                                query
                            ));
                        }
                    }
                }
            }

            result.Add(GetExpression(argument, filter, entityType, selection, model));

            return result;
        }

        private static Dictionary<string, Field> ToDictionary(KeyValuePair<string, Field> field)
            => field.Value.SelectionSet.Selections.ToDictionary(x => (x as Field).Name, x => x as Field);

        private static Expression GetExpression(Expression argument, string filter, Type entityType, IDictionary<string, Field> selection, IModel model)
        {
            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            var compareToExpression = Expression.Constant($"%{filter}%");

            Expression clause = default;
            ResolveFieldContextHelpers
                .GetProperties(entityType, selection, model)
                .Where(x => Attribute.IsDefined(x, typeof(FilterableAttribute)))
                .ToList()
                .ForEach(field =>
                {
                    Expression property = Expression.MakeMemberAccess(argument, field);

                    if (field.PropertyType != typeof(string))
                    {
                        property = Expression.Call(property, convertToStringMethod);
                    }

                    property = Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), property, compareToExpression);

                    if (clause != null)
                    {
                        clause = Expression.Or(clause, property);
                    }
                    else
                    {
                        clause = property;
                    }
                });

            return clause;
        }

        private static MethodInfo GetWhereMethod() => typeof(Queryable)
            .GetMethods()
            .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();

        private static MethodInfo GetAnyMethod() => typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == "Any" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
    }
}