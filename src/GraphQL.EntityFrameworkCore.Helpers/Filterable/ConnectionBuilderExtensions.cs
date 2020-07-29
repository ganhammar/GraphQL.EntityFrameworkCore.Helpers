using GraphQL.Builders;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder<TSourceType> Filterable<TSourceType>(this ConnectionBuilder<TSourceType> builder)
        {
            builder.Argument<FilterableInputGraphType>("filter", string.Empty);

            return builder;
        }
    }
}