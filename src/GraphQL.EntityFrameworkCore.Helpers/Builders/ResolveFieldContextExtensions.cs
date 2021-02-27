using System;
using GraphQL.Utilities;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class ResolveFieldContextExtensions
    {
        private static readonly string _exceptionMessage = "ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on";

        public static T GetService<T>(this IResolveFieldContext<object> context)
        {
            if (context.RequestServices == default)
            {
                throw new Exception(_exceptionMessage);
            }

            return context.RequestServices.GetRequiredService<T>();
        }

        public static object GetService(this IResolveFieldContext<object> context, Type type)
        {
            if (context.RequestServices == default)
            {
                throw new Exception(_exceptionMessage);
            }

            return context.RequestServices.GetRequiredService(type);
        }
    }
}