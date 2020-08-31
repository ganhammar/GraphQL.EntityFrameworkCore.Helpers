using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GraphQL.Builders;
using GraphQL.DataLoader;
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

        public static FieldQueryBuilder<TSourceType, object, TDbContext, TProperty> FromDbContext<TSourceType, TDbContext, TProperty>(
                this FieldBuilder<TSourceType, object> builder,
                TDbContext dbContext,
                Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
            where TDbContext : DbContext
            where TProperty : class
        {
            var queryBuilder = new FieldQueryBuilder<TSourceType, object, TDbContext, TProperty>(builder, dbContext);

            queryBuilder.Set(accessor);

            return queryBuilder;
        }

        public static FieldQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty> FromDbContext<TSourceType, TReturnType, TDbContext, TProperty>(
                this FieldBuilder<TSourceType, TReturnType> builder,
                TDbContext dbContext,
                Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
            where TDbContext : DbContext
            where TProperty : class
        {

            var queryBuilder = new FieldQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty>(builder, dbContext);

            queryBuilder.Set(accessor);

            return queryBuilder;
        }

        public static BatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey> Include<TSourceType, TReturnType, TDbContext, TProperty, TKey>(
                this FieldBuilder<TSourceType, TReturnType> field,
                IDataLoaderContextAccessor dataLoaderContextAccessor,
                TDbContext dbContext,
                Expression<Func<TSourceType, TProperty>> propertyToInclude,
                Expression<Func<TSourceType, TKey>> keyProperty)
            where TDbContext : DbContext
        {
            return new BatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>(
                field, dbContext, dataLoaderContextAccessor, propertyToInclude, keyProperty);
        }
    }
}