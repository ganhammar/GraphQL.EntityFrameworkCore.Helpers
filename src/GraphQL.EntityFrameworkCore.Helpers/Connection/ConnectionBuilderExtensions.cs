using GraphQL.Builders;
using GraphQL.EntityFrameworkCore.Helpers.Filterable;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder<TSourceType> Paged<TSourceType>(this ConnectionBuilder<TSourceType> builder, int pageSize = 20)
        {
            builder.Bidirectional();
            builder.PageSize(pageSize);
            builder.Argument<BooleanGraphType>("isAsc", "Order items ascending if true");
            builder.Argument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("orderBy", "Fields to order by");

            return builder.Filterable();
        }
    }
}