using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
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
            
            // Input parameter
            var parameter = Expression.Parameter(entityType);
            
            // New statement, new TQuery()
            var newEntity = Expression.New(entityType);

            // Set value, Field = Param.Field
            var bindings = properties.Select(propertyType =>
                Expression.Bind(propertyType, Expression.Property(parameter, propertyType)));

            // Initialization, new TQuery { Field = Param.Field, ... }
            var initializeEntity = Expression.MemberInit(newEntity, bindings);

            // Lambda expression, x => new TQuery { Field = Param.Field, ... }
            var lambda = Expression.Lambda<Func<TQuery, TQuery>>(initializeEntity, parameter);

            return query.Select(lambda);
        }
    }
}