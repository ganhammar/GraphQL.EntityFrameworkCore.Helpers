using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
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

            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            var entityType = typeof(TQuery);

            var arg = Expression.Parameter(entityType, "x");
            var compareToExpression = Expression.Constant($"%{filter}%");

            Expression clause = null;
            ResolveFieldContextHelpers
                .GetProperties(entityType, context.SubFields, model)
                .Where(x => Attribute.IsDefined(x, typeof(FilterableAttribute)))
                .ToList()
                .ForEach(field =>
                {
                    Expression property = Expression.MakeMemberAccess(arg, field);

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

            if (clause == null)
            {
                return query;
            }

            clause = Expression.Lambda(clause, arg);

            var method = GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType);

            return (IQueryable<TQuery>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static MethodInfo GetWhereMethod() => typeof(System.Linq.Queryable)
            .GetMethods()
            .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
    }
}