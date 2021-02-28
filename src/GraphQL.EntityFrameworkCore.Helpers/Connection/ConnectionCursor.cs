using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class ConnectionCursor
    {
        public static readonly string DateTimeFormatPattern = "yyyy-MM-dd HH:mm:ss.fffffff";
        public static readonly string DateTimeOffsetFormatPattern = "yyyy-MM-dd HH:mm:ss.fffffff zzz";

        public static Func<TSourceType, object> GetLambdaForCursor<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
        {
            var objectToString = typeof(object).GetMethod("ToString");
            var dateTimeToString = typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) });
            var dateTimeOffsetToString = typeof(DateTimeOffset).GetMethod("ToString", new Type[] { typeof(string) });
            var dateTimeFormat = Expression.Constant(DateTimeFormatPattern);
            var dateTimeOffsetFormat = Expression.Constant(DateTimeOffsetFormatPattern);

            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

            var entityType = typeof(TSourceType);

            ParameterExpression arg = Expression.Parameter(entityType);
            Expression selector = null;

            var orderBy = GetOrderBy<TSourceType, TReturnType>(request, model);
            orderBy.ForEach(field =>
            {
                var propertyInfo = entityType.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                Expression property = Expression.Property(arg, field);

                if (propertyInfo.PropertyType.IsGenericType && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = propertyInfo.PropertyType.GetGenericArguments()[0];

                    property = Expression.Convert(
                        Expression.Coalesce(property, Expression.Default(underlyingType)),
                        underlyingType);
                }

                if (propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?))
                {
                    property = Expression.Call(property, dateTimeToString, dateTimeFormat);
                }
                else if (propertyInfo.PropertyType == typeof(DateTimeOffset) || propertyInfo.PropertyType == typeof(DateTimeOffset?))
                {
                    property = Expression.Call(property, dateTimeOffsetToString, dateTimeOffsetFormat);
                }
                else if (propertyInfo.PropertyType != typeof(string))
                {
                    property = Expression.Call(property, objectToString);
                }

                if (selector != null)
                {
                    selector = Expression.Add(
                        selector,
                        property,
                        concatMethod);
                }
                else
                {
                    selector = property;
                }
            });

            return (Func<TSourceType, object>)Expression.Lambda(selector, new ParameterExpression[] { arg }).Compile();
        }

        public static List<string> GetOrderBy<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
        {
            var orderBy = request.OrderBy != default ? request.OrderBy.ToList() : new List<string>();
            var entityType = model.FindEntityType(typeof(TSourceType));
            var primaryKeys = entityType?.FindPrimaryKey().Properties
                    .Select(x => x.Name)
                    .ToList();
            var sourceType = typeof(TSourceType);

            var hasUniqueColumn = false;
            orderBy.ForEach(x =>
            {
                var sourceProperty = sourceType.GetProperty(x, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (sourceProperty == default)
                {
                    return;
                }

                var property = entityType?.FindProperty(sourceProperty.Name);
                var isUnique = Attribute.IsDefined(sourceProperty, typeof(UniqueAttribute));

                if ((property != default && (property.IsPrimaryKey() || entityType.FindForeignKeys(property).Where(y => y.IsUnique).Any()))
                    || isUnique || new[] { typeof(Guid), typeof(DateTime), typeof(DateTimeOffset) }.Contains(sourceProperty.PropertyType))
                {
                    hasUniqueColumn = true;
                }
            });

            if (!hasUniqueColumn && primaryKeys != default && primaryKeys.Any())
            {
                orderBy.AddRange(primaryKeys);
            }

            return orderBy;
        }
        
        public static (string firstCursor, string lastCursor) GetFirstAndLastCursor<TType, TCursor>(
            IEnumerable<TType> enumerable,
            Func<TType, TCursor> getCursorProperty)
        {
            if (getCursorProperty == null)
            {
                throw new ArgumentNullException(nameof(getCursorProperty));
            }

            if (enumerable == null || enumerable.Count() == 0)
            {
                return (null, null);
            }

            var firstCursor = ToCursor(getCursorProperty(enumerable.First()));
            var lastCursor = ToCursor(getCursorProperty(enumerable.Last()));

            return (firstCursor, lastCursor);
        }

        public static Type GetCursorType<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel dbModel)
        {
            var orderBy = GetOrderBy<TSourceType, TReturnType>(request, dbModel);
            if (orderBy == null || orderBy.Count == 0)
            {
                throw new Exception("Order by is not defined");
            }

            if (orderBy.Count > 1)
            {
                return typeof(string);
            }

            var model = typeof(TSourceType);
            var property = model.GetProperty(orderBy.First(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                throw new Exception("Requested order by is not valid");
            }

            return property.PropertyType;
        }
        
        public static string GetCursor<TType, TCursor>(TType item, Func<TType, TCursor> getCursorProperty)
            => ToCursor(getCursorProperty(item));

        public static string ToCursor<T>(T value) => Base64Encode(value.ToString());

        public static T FromCursor<T>(string cursor)
        {
            if (string.IsNullOrEmpty(cursor))
            {
                return default;
            }

            string decodedValue;
            try
            {
                decodedValue = Base64Decode(cursor);
            }
            catch (FormatException)
            {
                return default;
            }

            return (T)Convert.ChangeType(decodedValue, typeof(T), CultureInfo.InvariantCulture);
        }

        private static string Base64Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

        private static string Base64Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}