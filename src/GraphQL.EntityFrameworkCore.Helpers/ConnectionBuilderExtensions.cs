using System;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder<TSourceType> Paged<TSourceType>(this ConnectionBuilder<TSourceType> builder, int pageSize = 20)
        {
            builder.Bidirectional();
            builder.PageSize(pageSize);
            builder.Argument<BooleanGraphType>("isAsc", "Order items ascending if true");
            builder.Argument<ListGraphType<NonNullGraphType<StringGraphType>>>("orderBy", "Fields to order by");

            return builder.Filterable();
        }

        public static ConnectionBuilder<TSourceType> Filterable<TSourceType>(this ConnectionBuilder<TSourceType> builder)
        {
            builder.Argument<FilterableInputGraphType>("filter", string.Empty);

            return builder;
        }

        public static ConnectionBuilder<TSourceType> ResolveConnectionAsync<TSourceType, TDbContext, TProperty>(
            this ConnectionBuilder<TSourceType> builder, TDbContext dbContext, Expression<Func<TDbContext, DbSet<TProperty>>> accessor, Type connectionInputType)
            where TDbContext : DbContext
            where TProperty : class
        {
            builder.Paged();
            
            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            builder.ResolveAsync(async context =>
            {
                var query = typeof(DbContext).GetMethod(nameof(DbContext.Set))
                    .MakeGenericMethod(type)
                    .Invoke(dbContext, null);

                var input = (IConnectionInput<TProperty>)Activator.CreateInstance(connectionInputType);
                input.SetConnectionInput((IResolveConnectionContext<object>)context);

                return await ((IQueryable<TProperty>)query)
                    .ToConnection(input, dbContext.Model);
            });

            return builder;
        }
    }
}