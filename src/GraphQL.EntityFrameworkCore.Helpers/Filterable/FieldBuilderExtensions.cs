using System;
using System.Linq.Expressions;
using GraphQL.Builders;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FieldBuilderExtensions
    {
        public static FieldBuilder<TSourceType, TReturnType> Filterable<TSourceType, TReturnType>(this FieldBuilder<TSourceType, TReturnType> builder)
        {
            builder.Argument<FilterableInputGraphType>("filter");

            return builder;
        }

        public static FieldBuilder<TSourceType, TReturnType> FilterableProperty<TSourceType, TReturnType, TProperty>(this FieldBuilder<TSourceType, TReturnType> field, Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);
            FilterableHelpers.Add(type, property);

            return field;
        }
    }
}