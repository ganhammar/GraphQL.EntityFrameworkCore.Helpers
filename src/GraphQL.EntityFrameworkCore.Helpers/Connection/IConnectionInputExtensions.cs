using System;
using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionInputExtensions
    {
        public static ConnectionValidationResult IsValid<TModel, TRequest>(this IConnectionInput<TRequest> request)
        {
            var result = new ConnectionValidationResult();

            if (request.First == default)
            {
                result.IsValid = false;
                result.Errors.Add("First is required");
            }

            if (request.OrderBy == default || IsOrderByValid<TModel, TRequest>(request) == false)
            {
                result.IsValid = false;
                result.Errors.Add(request.OrderBy == default
                    ? "Order by is not defined"
                    : "Cannot order by one or more of the provided fields");

                return result;
            }

            var (_, _, isAfter, isBefore) = QueryableExtensions.GetPointer<TModel, TRequest>(request);

            if (isAfter && IsAfterValid<TModel, TRequest>(request) == false)
            {
                result.IsValid = false;
                result.Errors.Add("The after cursor is not valid");
            }

            if (isBefore && IsBeforeValid<TModel, TRequest>(request) == false)
            {
                result.IsValid = false;
                result.Errors.Add("The before cursor is not valid");
            }

            return result;
        }

        public class ConnectionValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new List<string>();
        }

        private static bool IsBeforeValid<TModel, TRequest>(IConnectionInput<TRequest> request)
            => IsCursorDefined<TModel, TRequest>(request, request.After) == false;

        private static bool IsAfterValid<TModel, TRequest>(IConnectionInput<TRequest> request)
            => IsCursorDefined<TModel, TRequest>(request, request.Before) == false;

        private static bool IsCursorDefined<TModel, TRequest>(IConnectionInput<TRequest> request, string cursor)
        {
            var type = GetOrderByType<TModel, TRequest>(request);
            var value = ConnectionCursor.FromCursor<object>(cursor);

            return value != null && (type == typeof(string) || !value.Equals(Activator.CreateInstance(type)));
        }

        private static bool IsOrderByValid<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            var model = typeof(TModel);

            foreach (var field in request.OrderBy)
            {
                if (model.GetProperty(field) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetOrderByType<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            if (request.OrderBy == default || request.OrderBy.Length == 0)
            {
                return null;
            }

            Type type = null;
            try
            {
                type = ConnectionCursor.GetCursorType<TModel, TRequest>(request);
            }
            catch
            { }

            return type;
        }
    }
}