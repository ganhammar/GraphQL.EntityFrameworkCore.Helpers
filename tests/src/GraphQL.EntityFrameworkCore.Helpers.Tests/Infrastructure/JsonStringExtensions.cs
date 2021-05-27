using GraphQL.SystemTextJson;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public static class JsonStringExtensions
    {
        public static ExecutionResult ToExecutionResult(this string json, ExecutionErrors errors = null, bool executed = true)
            => new ExecutionResult
            {
                Data = string.IsNullOrWhiteSpace(json) ? null : json.ToInputs(),
                Errors = errors,
                Executed = executed
            };
    }
}