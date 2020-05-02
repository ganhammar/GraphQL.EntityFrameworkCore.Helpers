using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionInputExtensions
    {
        public static void SetConnectionInput<TReturnType>(this IConnectionInput<TReturnType> input,
            IResolveConnectionContext<object> context)
        {
            var isAsc = context.GetArgument<bool?>("isAsc");

            input.First = context.First ?? default(int);
            input.After = context.After;
            input.Before = context.Before;
            input.IsAsc = isAsc != null ? isAsc.Value : true;
            input.OrderBy = context.GetArgument<string[]>("orderBy");
            input.Filter = context.GetArgument<string>("filter");
            input.Context = context;
        }

        public static ConnectionValidationResult IsValid<TSourceType, TReturnType>(this IConnectionInput<TReturnType> request, IModel model)
        {
            var result = new ConnectionValidationResult();

            if (request.First == default)
            {
                result.IsValid = false;
                result.Errors.Add(new ConnectionError("First", "First is required"));
            }

            var orderBy = ConnectionCursor.GetOrderBy<TSourceType, TReturnType>(request, model);
            if (orderBy.Any() == false || IsOrderByValid<TSourceType, TReturnType>(orderBy) == false)
            {
                result.IsValid = false;
                result.Errors.Add(new ConnectionError("OrderBy", orderBy.Any() == false
                    ? "Order by is not defined"
                    : "Cannot order by one or more of the provided fields"));

                return result;
            }

            var (_, _, isAfter, isBefore) = QueryableExtensions.GetPointer<TSourceType, TReturnType>(request, model);

            if (isAfter && IsAfterValid<TSourceType, TReturnType>(request, model) == false)
            {
                result.IsValid = false;
                result.Errors.Add(new ConnectionError("After", "The after cursor is not valid"));
            }

            if (isBefore && IsBeforeValid<TSourceType, TReturnType>(request, model) == false)
            {
                result.IsValid = false;
                result.Errors.Add(new ConnectionError("Before", "The before cursor is not valid"));
            }

            return result;
        }

        public class ConnectionValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<ConnectionError> Errors { get; set; } = new List<ConnectionError>();
        }

        public class ConnectionError
        {
            public ConnectionError(string fieldName, string message)
            {
                FieldName = fieldName;
                Message = message;
            }

            public string FieldName { get; set; }
            public string Message { get; set; }
        }

        private static bool IsBeforeValid<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
            => IsCursorDefined<TSourceType, TReturnType>(request, request.After, model) == false;

        private static bool IsAfterValid<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
            => IsCursorDefined<TSourceType, TReturnType>(request, request.Before, model) == false;

        private static bool IsCursorDefined<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, string cursor, IModel model)
        {
            var type = GetOrderByType<TSourceType, TReturnType>(request, model);
            var value = ConnectionCursor.FromCursor<object>(cursor);

            return value != null && (type == typeof(string) || !value.Equals(Activator.CreateInstance(type)));
        }

        private static bool IsOrderByValid<TSourceType, TReturnType>(List<string> orderBy)
        {
            var model = typeof(TSourceType);

            foreach (var field in orderBy)
            {
                if (model.GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetOrderByType<TSourceType, TReturnType>(IConnectionInput<TReturnType> request, IModel model)
        {
            if (request.OrderBy == default || request.OrderBy.Length == 0)
            {
                return null;
            }

            Type type = null;
            try
            {
                type = ConnectionCursor.GetCursorType<TSourceType, TReturnType>(request, model);
            }
            catch
            { }

            return type;
        }
    }
}