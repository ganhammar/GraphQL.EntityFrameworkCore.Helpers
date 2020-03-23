using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Types.Relay.DataObjects;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class QueryableExtensions
    {
        public static async Task<Connection<TModel>> AsConnection<TModel, TRequest>(this IQueryable<TModel> query, IConnectionInput<TRequest> request)
        {
            var connection = new Connection<TModel>
            {
                PageInfo = new PageInfo(),
                Edges = new List<Edge<TModel>>(),
            };

            // Filter list based on Filter property
            query = query.ApplyFilter(request);

            connection.TotalCount = await query.CountAsync();

            var isAsc = request.IsAsc;
            
            var (after, before, isAfter, isBefore) = GetPointer<TModel, TRequest>(request);

            var anyItemsBefore = false;

            // Not first page
            if (isAfter || isBefore)
            {
                query = query.Where(request);

                anyItemsBefore = await query.AnyAsync();
            }

            var anyItemsAfter = await (isAsc ? query.OrderBy(request) : query.OrderByDescending(request))
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

            query = isAsc ? query.OrderBy(request) : query.OrderByDescending(request);

            if (isBefore)
            {
                var count = await query.CountAsync();
                query = query.Skip(count > request.First ? count - request.First : 0);
            }
            else
            {
                query = query.Take(request.First);
            }

            if (request.Context != default)
            {
                query.Select(request.Context);
            }

            var items = await query.ToListAsync();
            var lambda = ConnectionCursor.GetLambdaForCursor<TModel, TRequest>(request);
            var (startCursor, endCursor) = ConnectionCursor.GetFirstAndLastCursor(items, lambda);

            connection.PageInfo.StartCursor = startCursor;
            connection.PageInfo.EndCursor = endCursor;

            connection.Edges.AddRange(items.Select(x => new Edge<TModel>
            {
                Node = x,
                Cursor = ConnectionCursor.GetCursor(x, lambda),
            }));

            return connection;
        }

        private static (object after, object before, bool isAfter, bool isBefore) GetPointer<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            var cursorType = ConnectionCursor.GetCursorType<TModel, TRequest>(request);
            var defaultValue = cursorType == typeof(string) ? string.Empty : Activator.CreateInstance(cursorType);
            var before = ConnectionCursor.FromCursor<object>(request.Before);
            var after = ConnectionCursor.FromCursor<object>(request.After);
            var isBefore = request.Before != null && before != defaultValue;
            var isAfter = request.After != null && after != defaultValue;

            return (after, before, isAfter, isBefore);
        }

        private static IOrderedQueryable<TQuery> OrderBy<TQuery, TModel>(this IQueryable<TQuery> query, IConnectionInput<TModel> request)
            => GetOrderBy(query, request, "OrderBy");

        private static IOrderedQueryable<TQuery> OrderByDescending<TQuery, TModel>(this IQueryable<TQuery> query, IConnectionInput<TModel> request)
            => GetOrderBy(query, request, "OrderByDescending");

        private static IQueryable<TModel> Where<TModel, TRequest>(this IQueryable<TModel> query, IConnectionInput<TRequest> request)
        {
            var isAsc = request.IsAsc;
            var cursorType = ConnectionCursor.GetCursorType<TModel, TRequest>(request);
            var entityType = typeof(TModel);

            var (after, before, isAfter, isBefore) = GetPointer<TModel, TRequest>(request);
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
            var selector = GetLambda(query, request, arg);

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

            return (IQueryable<TModel>)genericMethod
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

        private static Expression GetLambda<TQuery, TModel>(IQueryable<TQuery> query, IConnectionInput<TModel> request, ParameterExpression arg)
        {
            var convertToString = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

            var modelEntityType = typeof(TModel);
            var queryEntityType = typeof(TQuery);

            Expression selector = null;
            request.OrderBy.ToList().ForEach(field =>
            {
                var propertyInfo = queryEntityType.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                Expression property = Expression.Property(arg, field);

                if (propertyInfo.PropertyType != typeof(string) && request.OrderBy.Length > 1)
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

        private static IOrderedQueryable<TModel> GetOrderBy<TModel, TRequest>(this IQueryable<TModel> query, IConnectionInput<TRequest> request, string methodName)
        {
            var cursorType = ConnectionCursor.GetCursorType<TModel, TRequest>(request);
            var entityType = typeof(TModel);

            ParameterExpression arg = Expression.Parameter(entityType, "x");

            var selector = Expression.Lambda(GetLambda(query, request, arg), new ParameterExpression[] { arg });

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

            return (IOrderedQueryable<TModel>)genericMethod
                .Invoke(genericMethod, new object[] { query, selector });
        }

        private static IQueryable<TQuery> ApplyFilter<TQuery, TModel>(this IQueryable<TQuery> query, IConnectionInput<TModel> request)
        {
            if (string.IsNullOrEmpty(request.Filter))
            {
                return query;
            }

            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            var queryEntityType = typeof(TQuery);

            var arg = Expression.Parameter(queryEntityType, "x");
            var compareToExpression = Expression.Constant($"%{request.Filter}%");

            Expression clause = null;
            queryEntityType
                .GetProperties()
                .Where(x => Attribute.IsDefined(x, typeof(FilterableAttribute)))
                .ToList()
                .ForEach(field =>
                {
                    Expression property = Expression.MakeMemberAccess(arg, field);

                    if (field.PropertyType != typeof(string))
                    {
                        property = Expression.Call(property, convertToStringMethod);
                    }

                    property = Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), property, compareToExpression);

                    if (clause != null)
                    {
                        clause = Expression.Or(clause, property);
                    }
                    else
                    {
                        clause = property;
                    }
                });

            if (clause == null)
            {
                return query;
            }

            clause = Expression.Lambda(clause, arg);

            var method = GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(queryEntityType);

            return (IQueryable<TQuery>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static MethodInfo GetWhereMethod() => typeof(System.Linq.Queryable)
            .GetMethods()
            .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
    }
}