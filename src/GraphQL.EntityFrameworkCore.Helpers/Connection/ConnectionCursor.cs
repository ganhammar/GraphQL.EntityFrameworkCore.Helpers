using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionCursor
    {
        public static readonly string DateTimeFormatPattern = "yyyy-MM-dd HH:mm:ss.fffffff";
        public static readonly string DateTimeOffsetFormatPattern = "yyyy-MM-dd HH:mm:ss.fffffff zzz";

        public static Func<TModel, object> GetLambdaForCursor<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            var objectToString = typeof(object).GetMethod("ToString");
            var dateTimeToString = typeof(DateTime).GetMethod("ToString", new Type[] { typeof(string) });
            var dateTimeOffsetToString = typeof(DateTimeOffset).GetMethod("ToString", new Type[] { typeof(string) });
            var dateTimeFormat = Expression.Constant(DateTimeFormatPattern);
            var dateTimeOffsetFormat = Expression.Constant(DateTimeOffsetFormatPattern);

            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

            var entityType = typeof(TModel);

            ParameterExpression arg = Expression.Parameter(entityType, "x");
            Expression selector = null;

            request.OrderBy.ToList().ForEach(field =>
            {
                var propertyInfo = entityType.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                Expression property = Expression.Property(arg, field);

                if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    property = Expression.Call(property, dateTimeToString, dateTimeFormat);
                }
                else if (propertyInfo.PropertyType == typeof(DateTimeOffset))
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
                        concatMethod
                    );
                }
                else
                {
                    selector = property;
                }
            });

            return (Func<TModel, object>)Expression.Lambda(selector, new ParameterExpression[] { arg }).Compile();
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

        public static Type GetCursorType<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            if (request.OrderBy == null || request.OrderBy.Length == 0)
            {
                throw new Exception("Order by is not defined");
            }

            if (request.OrderBy.Length > 1)
            {
                return typeof(string);
            }

            var model = typeof(TModel);
            var property = model.GetProperty(request.OrderBy.First(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

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