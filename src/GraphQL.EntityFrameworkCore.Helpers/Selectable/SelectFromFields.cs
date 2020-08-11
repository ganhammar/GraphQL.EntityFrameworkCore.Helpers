using System;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers.Selectable
{
    public static class SelectFromFields
    {
        public static IQueryable<TQuery> SelectFromContext<TQuery>(this IQueryable<TQuery> query, IResolveFieldContext<object> context, IModel model)
        {
            if (context == default)
            {
                return query;
            }

            query = query.Filter(context, model);

            var entityType = typeof(TQuery);
            
            // The requested properties
            var properties = ResolveFieldContextHelpers.GetProperties(entityType, context.SubFields, model);
            
            // Input parameter, x
            var parameter = Expression.Parameter(entityType, "x");
            
            // New statement, new TQuery()
            var newEntity = Expression.New(entityType);

            // Set value, Field = x.Field
            var bindings = properties.Select(propertyType =>
                Expression.Bind(propertyType, Expression.Property(parameter, propertyType)));

            // Initialization, new TQuery { Field = x.Field, ... }
            var initializeEntity = Expression.MemberInit(newEntity, bindings);

            // Lambda expression, x => new TQuery { Field = x.Field, ... }
            var lambda = Expression.Lambda<Func<TQuery, TQuery>>(initializeEntity, parameter);

            return query.Select(lambda);
        }
    }
}