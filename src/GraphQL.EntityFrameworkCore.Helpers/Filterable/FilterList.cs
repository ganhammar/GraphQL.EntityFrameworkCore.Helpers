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

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FilterList
    {
        public static IQueryable<TQuery> Filter<TQuery>(this IQueryable<TQuery> query, IResolveFieldContext<object> context, IModel model)
        {
            var filter = GetFilter(context);
            
            if (filter == default)
            {
                return query;
            }

            return AddWhere(query, filter, context, model);
        }

        private static FilterableInput GetFilter(IResolveFieldContext<object> context)
        {
            if (context == default)
            {
                return default;
            }

            var fields = GetOperationBranch(context.Operation.SelectionSet, context.FieldName);

            foreach (Field field in fields)
            {
                var filter = field.Arguments?.FirstOrDefault(x => x.Name.ToLower() == "filter");

                if (filter != default)
                {
                    var filterInputReference = (VariableReference)filter.Children.First(x => x.GetType() == typeof(VariableReference));
                    var input = GetFilterableInput(filterInputReference.Name, context);

                    if (context.FieldName == field.Name || input.Mode == FilterableModes.Deep)
                    {
                        return input;
                    }
                }
            }

            return default;
        }

        private static FilterableInput GetFilterableInput(string inputName, IResolveFieldContext<object> context)
        {
            var variable = context.Variables.First(x => x.Name == inputName);
            var arguments = variable.Value as IDictionary<string, object>;

            return (FilterableInput)arguments.ToObject(typeof(FilterableInput));
        }

        private static List<Field> GetOperationBranch(SelectionSet operation, string target)
        {
            var result = new List<Field>();

            foreach (Field selection in operation.Selections)
            {
                if (selection.Name == target)
                {
                    result.Add(selection);
                }
                else
                {
                    var children = GetOperationBranch(selection.SelectionSet, target);

                    if (children.Any())
                    {
                        result.AddRange(children);
                        result.Add(selection);
                    }
                }
            }

            return result;
        }

        private static IQueryable<TQuery> AddWhere<TQuery>(this IQueryable<TQuery> query, FilterableInput filter, IResolveFieldContext<object> context, IModel model)
        {
            var entityType = typeof(TQuery);
            var argument = Expression.Parameter(entityType);
            var expressions = GetSelectionPaths(argument, filter.Fields, entityType, context.SubFields, model, query);

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

        private static List<Expression> GetSelectionPaths<TQuery>(Expression argument, IEnumerable<FilterableInputField> fields, Type entityType, IDictionary<string, Field> selection, IModel model, IQueryable<TQuery> query)
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
                                GetFilterFields(field.Value.Name, fields),
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
                                GetFilterFields(field.Value.Name, fields),
                                targetType,
                                ToDictionary(field),
                                model,
                                query
                            ));
                        }
                    }
                }
            }

            var expression = GetExpression(argument, fields, entityType, selection, model);
            if (expression != default)
            {
                result.Add(expression);
            }

            return result;
        }

        private static IEnumerable<FilterableInputField> GetFilterFields(string fieldName, IEnumerable<FilterableInputField> fields)
        {
            var subFilter = fields.FirstOrDefault(x => x.Target.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

            if (subFilter == default)
            {
                return fields.Where(x => x.Target.Equals("All", StringComparison.InvariantCultureIgnoreCase));
            }
            
            return subFilter.Fields;
        }

        private static string GetFilterValueForField(string fieldName, IEnumerable<FilterableInputField> fields)
            => fields.FirstOrDefault(x => x.Target.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase))?.Value 
                ?? fields.FirstOrDefault(x => x.Target.Equals("All", StringComparison.InvariantCultureIgnoreCase))?.Value;

        private static Dictionary<string, Field> ToDictionary(KeyValuePair<string, Field> field)
            => field.Value.SelectionSet.Selections.ToDictionary(x => (x as Field).Name, x => x as Field);

        private static Expression GetExpression(Expression argument, IEnumerable<FilterableInputField> fields, Type entityType, IDictionary<string, Field> selection, IModel model)
        {
            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            Expression clause = default;
            ResolveFieldContextHelpers
                .GetProperties(entityType, selection, model)
                .Where(x => Attribute.IsDefined(x, typeof(FilterableAttribute)))
                .ToList()
                .ForEach(field =>
                {
                    var filter = GetFilterValueForField(field.Name, fields);

                    if (filter == default)
                    {
                        return;
                    }

                    var compareToExpression = Expression.Constant($"%{filter}%");
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