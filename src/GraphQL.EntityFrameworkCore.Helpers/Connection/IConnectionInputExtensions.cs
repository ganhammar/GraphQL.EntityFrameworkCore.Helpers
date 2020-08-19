using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
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
            input.Context = context;
        }

        public static ValidationResult Validate<TSourceType, TReturnType>(this IConnectionInput<TReturnType> request, IModel model)
        {
            var result = new ValidationResult();

            if (request.First == default)
            {
                result.Failures.Add(new ValidationFailure("First", "First is required"));
            }

            var orderBy = ConnectionCursor.GetOrderBy<TSourceType, TReturnType>(request, model);
            if (orderBy.Any() == false || IsOrderByValid<TSourceType, TReturnType>(orderBy) == false)
            {
                result.Failures.Add(new ValidationFailure("OrderBy", orderBy.Any() == false
                    ? "Order by is not defined"
                    : "Cannot order by one or more of the provided fields"));

                return result;
            }

            var (_, _, isAfter, isBefore) = QueryableExtensions.GetPointer<TSourceType, TReturnType>(request, model);

            if (isAfter && IsAfterValid<TSourceType, TReturnType>(request, model) == false)
            {
                result.Failures.Add(new ValidationFailure("After", "The after cursor is not valid"));
            }

            if (isBefore && IsBeforeValid<TSourceType, TReturnType>(request, model) == false)
            {
                result.Failures.Add(new ValidationFailure("Before", "The before cursor is not valid"));
            }

            return result;
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