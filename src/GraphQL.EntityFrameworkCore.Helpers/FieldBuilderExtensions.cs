using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using GraphQL.Builders;

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
    }
}