using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder<TSourceType> Filterable<TSourceType>(this ConnectionBuilder<TSourceType> builder)
        {
            builder.Argument<StringGraphType>("filter", "String to filter the list by");

            return builder;
        }
    }
}