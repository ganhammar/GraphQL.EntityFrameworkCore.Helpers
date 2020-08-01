using System;
using System.Linq.Expressions;
using GraphQL.Builders;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldBuilderExtensions
    {

        public static FieldBuilder<TSourceType, TReturnType> Property<TSourceType, TReturnType, TProperty>(this FieldBuilder<TSourceType, TReturnType> field, Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = FieldHelpers.GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);

            return field;
        }
    }
}