using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldBuilderExtensions
    {
        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> MapsTo<TSourceType, TReturnType, TProperty>(
            this FieldBuilder<TSourceType, TReturnType> field, 
            Expression<Func<TSourceType, IEnumerable<TProperty>>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);

            return new HelperFieldBuilder<TSourceType, TReturnType, TProperty>(field.FieldType);
        }

        public static HelperFieldBuilder<TSourceType, TReturnType, TProperty> MapsTo<TSourceType, TReturnType, TProperty>(
            this FieldBuilder<TSourceType, TReturnType> field, 
            Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);

            return new HelperFieldBuilder<TSourceType, TReturnType, TProperty>(field.FieldType);
        }

        public static FieldBuilder<TSourceType, TReturnType> Filtered<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> builder)
        {
            builder.Argument<FilterableInputGraphType>("filter");

            return builder;
        }

        public static FieldBuilder<TSourceType, TReturnType> IsFilterable<TSourceType, TReturnType, TProperty>(
            this FieldBuilder<TSourceType, TReturnType> field,
            Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);
            FilterableHelpers.Add(type, property);

            return field;
        }

        public static FieldBuilder<TSourceType, TReturnType> IsFilterable<TSourceType, TReturnType>(
            this FieldBuilder<TSourceType, TReturnType> field)
        {
            var type = typeof(TSourceType);

            FilterableHelpers.Add(type, field.FieldType.Name);

            return field;
        }

        public static FieldBuilder<TSourceType, List<TReturnType>> ResolveListAsync<TSourceType, TReturnType, TDbContext>(
            this FieldBuilder<TSourceType, List<TReturnType>> builder, TDbContext dbContext, Expression<Func<TDbContext, DbSet<TReturnType>>> accessor)
            where TDbContext : DbContext
            where TReturnType : class
        {
            builder.Filtered();

            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            builder.ResolveAsync(async context =>
            {
                var query = typeof(DbContext).GetMethod(nameof(DbContext.Set))
                    .MakeGenericMethod(type)
                    .Invoke(dbContext, null);

                return await ((IQueryable<TReturnType>)query)
                    .SelectFromContext((IResolveFieldContext<object>)context, dbContext.Model)
                    .ToListAsync();
            });

            return builder;
        }

        public static FieldBuilder<TSourceType, object> ResolveListAsync<TSourceType, TDbContext, TProperty>(
            this FieldBuilder<TSourceType, object> builder, TDbContext dbContext, Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
            where TDbContext : DbContext
            where TProperty : class
        {
            builder.Filtered();

            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            builder.ResolveAsync(async context =>
            {
                var query = typeof(DbContext).GetMethod(nameof(DbContext.Set))
                    .MakeGenericMethod(type)
                    .Invoke(dbContext, null);

                return await ((IQueryable<TProperty>)query)
                    .SelectFromContext((IResolveFieldContext<object>)context, dbContext.Model)
                    .ToListAsync();
            });

            return builder;
        }
    }
}