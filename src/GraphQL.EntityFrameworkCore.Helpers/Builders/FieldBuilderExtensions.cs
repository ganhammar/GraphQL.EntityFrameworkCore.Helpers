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

        public static FieldQueryBuilder<TSourceType, object, TProperty> From<TSourceType, TDbContext, TProperty>(
                this FieldBuilder<TSourceType, object> builder,
                TDbContext _,
                Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
            where TDbContext : DbContext
            where TProperty : class
        {
            var targetType = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();
            var queryBuilder = new FieldQueryBuilder<TSourceType, object, TProperty>(builder, targetType, typeof(TDbContext));

            return queryBuilder;
        }

        public static FieldQueryBuilder<TSourceType, TReturnType, TProperty> From<TSourceType, TReturnType, TDbContext, TProperty>(
                this FieldBuilder<TSourceType, TReturnType> builder,
                TDbContext _,
                Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
            where TDbContext : DbContext
            where TProperty : class
        {
            var targetType = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();
            var queryBuilder = new FieldQueryBuilder<TSourceType, TReturnType, TProperty>(builder, targetType, typeof(TDbContext));

            return queryBuilder;
        }

        public static FieldQueryBuilder<TSourceType, object, TProperty> From<TSourceType, TProperty>(
                this FieldBuilder<TSourceType, object> builder,
                DbSet<TProperty> property)
            where TProperty : class
        {
            var targetType = property.GetType().GetGenericArguments().First();
            var queryBuilder = new FieldQueryBuilder<TSourceType, object, TProperty>(builder, targetType);

            return queryBuilder;
        }

        public static FieldQueryBuilder<TSourceType, TReturnType, TProperty> From<TSourceType, TReturnType, TProperty>(
                this FieldBuilder<TSourceType, TReturnType> builder,
                DbSet<TProperty> property)
            where TProperty : class
        {
            var targetType = property.GetType().GetGenericArguments().First();
            var queryBuilder = new FieldQueryBuilder<TSourceType, TReturnType, TProperty>(builder, targetType);

            return queryBuilder;
        }

        public static BatchQueryBuilder<TSourceType, TReturnType> Include<TSourceType, TReturnType, TDbContext>(
                this FieldBuilder<TSourceType, TReturnType> field,
                TDbContext _,
                Expression<Func<TSourceType, TReturnType>> propertyToInclude)
            where TDbContext : DbContext
            where TReturnType : class
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(propertyToInclude);

            FieldHelpers.Map(type, field.FieldType, property);

            return new BatchQueryBuilder<TSourceType, TReturnType>(
                field, propertyToInclude, typeof(TDbContext));
        }

        public static BatchQueryBuilder<TSourceType, TReturnType> Include<TSourceType, TReturnType>(
                this FieldBuilder<TSourceType, TReturnType> field,
                Expression<Func<TSourceType, TReturnType>> propertyToInclude)
            where TReturnType : class
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(propertyToInclude);

            FieldHelpers.Map(type, field.FieldType, property);

            return new BatchQueryBuilder<TSourceType, TReturnType>(
                field, propertyToInclude);
        }

        public static CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> Include<TSourceType, TReturnType, TDbContext, TProperty>(
                this FieldBuilder<TSourceType, IEnumerable<TReturnType>> field,
                TDbContext _,
                Expression<Func<TSourceType, IEnumerable<TProperty>>> collectionToInclude)
            where TDbContext : DbContext
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(collectionToInclude);

            FieldHelpers.Map(type, field.FieldType, property);

            return new CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty>(
                field, collectionToInclude, typeof(TDbContext));
        }

        public static CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> Include<TSourceType, TReturnType, TProperty>(
                this FieldBuilder<TSourceType, IEnumerable<TReturnType>> field,
                Expression<Func<TSourceType, IEnumerable<TProperty>>> collectionToInclude)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(collectionToInclude);

            FieldHelpers.Map(type, field.FieldType, property);

            return new CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty>(
                field, collectionToInclude);
        }
    }
}