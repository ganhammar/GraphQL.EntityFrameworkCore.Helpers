using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class QueryableExtensions
    {
        public static async Task<Connection<TReturnType>> ToConnection<TSourceType, TReturnType>(this IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, IModel model)
        {
            var validationResult = request.Validate<TSourceType, TReturnType>(model);
            if (validationResult.IsValid == false)
            {
                throw new Exception(validationResult.Failures.First().Message);
            }

            var connection = new Connection<TReturnType>
            {
                PageInfo = new PageInfo(),
                Edges = new List<Edge<TReturnType>>(),
            };

            // Filter list based on Filter property
            query = query.Filter(request.Context, model);

            connection.TotalCount = await query.CountAsync();

            var isAsc = request.IsAsc;

            var (after, before, isAfter, isBefore) = GetPointer<TSourceType, TReturnType>(request, model);

            var anyItemsBefore = false;

            // Not first page
            if (isAfter || isBefore)
            {
                query = query.Where(request, model);

                anyItemsBefore = await query.AnyAsync();
            }

            var anyItemsAfter = await (isAsc ? query.OrderBy(request, model) : query.OrderByDescending(request, model))
                .Skip(request.First).AnyAsync();

            if (isBefore)
            {
                connection.PageInfo.HasNextPage = anyItemsBefore;
                connection.PageInfo.HasPreviousPage = anyItemsAfter;
            }
            else
            {
                connection.PageInfo.HasNextPage = anyItemsAfter;
                connection.PageInfo.HasPreviousPage = anyItemsBefore;
            }

            query = isAsc ? query.OrderBy(request, model) : query.OrderByDescending(request, model);

            if (isBefore)
            {
                var count = await query.CountAsync();
                query = query.Skip(count > request.First ? count - request.First : 0);
            }
            else
            {
                query = query.Take(request.First);
            }

            query.SelectFromContext(request.Context, model);

            var items = await query.ToListAsync();
            var lambda = ConnectionCursor.GetLambdaForCursor<TSourceType, TReturnType>(request, model);
            var (startCursor, endCursor) = ConnectionCursor.GetFirstAndLastCursor(items, lambda);

            connection.PageInfo.StartCursor = startCursor;
            connection.PageInfo.EndCursor = endCursor;

            connection.Edges.AddRange(items.Select(x => new Edge<TReturnType>
            {
                Node = Create<TReturnType>(x),
                Cursor = ConnectionCursor.GetCursor(x, lambda),
            }));

            return connection;
        }

        public static (object after, object before, bool isAfter, bool isBefore) GetPointer<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
        {
            var cursorType = ConnectionCursor.GetCursorType<TSourceType, TReturnType>(request, model);
            var defaultValue = cursorType == typeof(string) ? string.Empty : Activator.CreateInstance(cursorType);
            var before = ConnectionCursor.FromCursor<object>(request.Before);
            var after = ConnectionCursor.FromCursor<object>(request.After);
            var isBefore = request.Before != null && before != defaultValue;
            var isAfter = request.After != null && after != defaultValue;

            return (after, before, isAfter, isBefore);
        }

        private static IOrderedQueryable<TSourceType> OrderBy<TSourceType, TReturnType>(this IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, IModel model)
            => GetOrderBy(query, request, "OrderBy", model);

        private static IOrderedQueryable<TSourceType> OrderByDescending<TSourceType, TReturnType>(this IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, IModel model)
            => GetOrderBy(query, request, "OrderByDescending", model);

        private static IQueryable<TSourceType> Where<TSourceType, TReturnType>(this IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, IModel model)
        {
            var isAsc = request.IsAsc;
            var cursorType = ConnectionCursor.GetCursorType<TSourceType, TReturnType>(request, model);
            var entityType = typeof(TSourceType);

            var (after, before, isAfter, isBefore) = GetPointer<TSourceType, TReturnType>(request, model);
            var isGreaterThan = isBefore ? !isAsc : isAsc;

            var value = isBefore ? before : after;
            if (cursorType == typeof(Guid))
            {
                value = new Guid(value as string);
            }
            else
            {
                var converter = TypeDescriptor.GetConverter(Nullable.GetUnderlyingType(cursorType) ?? cursorType);
                value = value.GetType() == typeof(string) ?
                    converter.ConvertFrom(value) :
                    Convert.ChangeType(value, Nullable.GetUnderlyingType(cursorType) ?? cursorType);
            }

            var compareToExpression = Expression.Constant(value);

            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            ParameterExpression arg = Expression.Parameter(entityType, "x");
            var selector = GetLambda(query, request, arg, model);

            if (cursorType == typeof(string) || cursorType == typeof(Guid))
            {
                var compareTo = cursorType == typeof(string)
                    ? typeof(string).GetMethod(nameof(string.CompareTo), new[] { typeof(string) })
                    : typeof(Guid).GetMethod(nameof(string.CompareTo), new[] { typeof(Guid) });

                selector = Expression.Call(selector, compareTo, compareToExpression);
                compareToExpression = Expression.Constant(0);
            }

            var clause = Expression.Lambda(
                isGreaterThan
                    ? NullableGreaterThan(selector, compareToExpression)
                    : NullableLessThan(selector, compareToExpression),
                new ParameterExpression[] { arg });

            var method = GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType);

            return (IQueryable<TSourceType>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static Expression NullableGreaterThan(Expression e1, Expression e2)
        {
            if (IsNullableType(e1.Type) && !IsNullableType(e2.Type))
            {
                e2 = Expression.Convert(e2, e1.Type);
            }
            else if (!IsNullableType(e1.Type) && IsNullableType(e2.Type))
            {
                e1 = Expression.Convert(e1, e2.Type);
            }

            return Expression.GreaterThan(e1, e2);
        }

        private static Expression NullableLessThan(Expression e1, Expression e2)
        {
            if (IsNullableType(e1.Type) && !IsNullableType(e2.Type))
            {
                e2 = Expression.Convert(e2, e1.Type);
            }
            else if (!IsNullableType(e1.Type) && IsNullableType(e2.Type))
            {
                e1 = Expression.Convert(e1, e2.Type);
            }

            return Expression.LessThan(e1, e2);
        }

        private static bool IsNullableType(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);

        private static Expression GetLambda<TSourceType, TReturnType>(IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, ParameterExpression arg, IModel model)
        {
            var convertToString = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

            var modelEntityType = typeof(TReturnType);
            var queryEntityType = typeof(TSourceType);

            Expression selector = null;
            var orderBy = ConnectionCursor.GetOrderBy<TSourceType, TReturnType>(request, model);
            orderBy.ForEach(field =>
            {
                var propertyInfo = queryEntityType.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                Expression property = Expression.Property(arg, field);

                if (propertyInfo.PropertyType != typeof(string) && orderBy.Count() > 1)
                {
                    property = Expression.Call(property, convertToString);
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

            return selector;
        }

        private static IOrderedQueryable<TSourceType> GetOrderBy<TSourceType, TReturnType>(this IQueryable<TSourceType> query, IConnectionInput<TReturnType> request, string methodName, IModel model)
        {
            var cursorType = ConnectionCursor.GetCursorType<TSourceType, TReturnType>(request, model);
            var entityType = typeof(TSourceType);

            ParameterExpression arg = Expression.Parameter(entityType, "x");

            var selector = Expression.Lambda(GetLambda(query, request, arg, model), new ParameterExpression[] { arg });

            var enumarableType = typeof(System.Linq.Queryable);

            var method = enumarableType.GetMethods()
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                .Where(m =>
                {
                    var parameters = m.GetParameters().ToList();
                    return parameters.Count == 2;
                })
                .Single();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType, cursorType);

            return (IOrderedQueryable<TSourceType>)genericMethod
                .Invoke(genericMethod, new object[] { query, selector });
        }

        public static MethodInfo GetWhereMethod() => typeof(Queryable)
            .GetMethods()
            .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
        
        public static MethodInfo GetAnyMethod() => typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == "Any" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
        
        public static MethodInfo GetToDictionaryAsyncMethod() => typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == "ToDictionaryAsync" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 3)
            .First();
        
        public static MethodInfo GetIncludeMethod() => typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == "Include" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();

        public static TReturnType Create<TReturnType>(object source)
        {
            var target = (TReturnType)Activator.CreateInstance(typeof(TReturnType));

            Type targetType = target.GetType();
            Type typeSource = source.GetType();

            PropertyInfo[] properties = typeSource.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.CanRead == false)
                {
                    continue;
                }

                PropertyInfo targetProperty = targetType.GetProperty(property.Name);

                if (targetProperty == default)
                {
                    var attribute = typeof(MapsFromAttribute);
                    targetProperty = targetType
                        .GetProperties()
                        .Where(x => Attribute.IsDefined(x, attribute))
                        .Where(x => ((MapsFromAttribute)x.GetCustomAttribute(attribute)).PropertyName == property.Name)
                        .FirstOrDefault();

                    if (targetProperty == default)
                    {
                        continue;
                    }
                }

                if (targetProperty.CanWrite == false)
                {
                    continue;
                }

                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }

                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }

                if (!targetProperty.PropertyType.IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }

                targetProperty.SetValue(target, property.GetValue(source, null), null);
            }

            return target;
        }
    }
}