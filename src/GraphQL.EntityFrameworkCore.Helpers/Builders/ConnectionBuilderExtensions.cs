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

        public static ConnectionQueryBuilder<TSourceType, TReturnType> From<TSourceType, TReturnType, TDbContext>(
                this ConnectionBuilder<TSourceType> builder,
                TDbContext _,
                Expression<Func<TDbContext, DbSet<TReturnType>>> accessor)
            where TDbContext : DbContext
            where TReturnType : class
        {
            var targetType = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();
            var queryBuilder = new ConnectionQueryBuilder<TSourceType, TReturnType>(builder, targetType, typeof(TDbContext));

            return queryBuilder;
        }

        public static ConnectionQueryBuilder<TSourceType, TReturnType> From<TSourceType, TReturnType>(
                this ConnectionBuilder<TSourceType> builder,
                DbSet<TReturnType> property)
            where TReturnType : class
        {
            var targetType = property.GetType().GetGenericArguments().First();
            var queryBuilder = new ConnectionQueryBuilder<TSourceType, TReturnType>(builder, targetType);

            return queryBuilder;
        }
    }
}