using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldBuilderExtensions
    {

        public static FieldBuilder<TSourceType, TReturnType> Property<TSourceType, TReturnType, TProperty>(this FieldBuilder<TSourceType, TReturnType> field, Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);
            var property = GetPropertyInfo(accessor);

            FieldHelpers.Map(type, field.FieldType, property);

            return field;
        }

        private static PropertyInfo GetPropertyInfo<TSourceType, TProperty>(Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);

            var member = accessor.Body as MemberExpression;
            if (member == null) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    accessor.ToString()));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    accessor.ToString()));
            }

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType)) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    accessor.ToString(),
                    type));
            }

            return propInfo;
        }
    }
}