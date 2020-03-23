using System;
using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionInputExtensions
    {
        public static ConnectionValidationResult IsValid<TModel, TRequest>(this IConnectionInput<TRequest> request)
        {
            var result = new ConnectionValidationResult();

            if (request.OrderBy == default || IsOrderByValid<TModel, TRequest>(request) == false)
            {
                result.IsValid = false;
                result.Errors.Add(request.OrderBy == default
                    ? "Order by is not defined"
                    : "Cannot order by one or more of the provided fields");
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

            if (request.First == default)
            {
                result.IsValid = false;
                result.Errors.Add("First is required");
            }

            return result;
        }

        public class ConnectionValidationResult
        {
            public bool IsValid { get; set; } = true;
            public List<string> Errors { get; set; } = new List<string>();
        }

        private static bool IsBeforeValid<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            var type = GetOrderByType<TModel, TRequest>(request);
            var after = ConnectionCursor.FromCursor<object>(request.After);

            if (type == null || (after != null && type != typeof(string)
                && !after.Equals(Activator.CreateInstance(type))))
            {
                return false;
            }

            return true;
        }

        private static bool IsAfterValid<TModel, TRequest>(IConnectionInput<TRequest> request)
        {
            var type = GetOrderByType<TModel, TRequest>(request);
            var before = ConnectionCursor.FromCursor<object>(request.Before);

            if (type == null || (before != null && type != typeof(string)
                && !before.Equals(Activator.CreateInstance(type))))
            {
                return false;
            }

            return true;
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
            if (request.OrderBy == null || request.OrderBy.Length == 0)
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