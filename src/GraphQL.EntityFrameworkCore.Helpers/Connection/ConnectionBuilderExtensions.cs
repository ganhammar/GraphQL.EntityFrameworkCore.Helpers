using GraphQL.Builders;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public static class ConnectionBuilderExtensions
    {
        public static ConnectionBuilder<TSourceType> Paged<TSourceType>(this ConnectionBuilder<TSourceType> connection, int pageSize = 20)
        {
            connection.Bidirectional();
            connection.PageSize(pageSize);
            connection.Argument<BooleanGraphType>("isAsc", "Order items ascending if true");
            connection.Argument<NonNullGraphType<ListGraphType<NonNullGraphType<StringGraphType>>>>("orderBy", "Fields to order by");

            return connection.Filterable();
        }

        public static ConnectionBuilder<TSourceType> Filterable<TSourceType>(this ConnectionBuilder<TSourceType> connection)
        {
            connection.Argument<StringGraphType>("filter", "String to filter the list by");

            return connection;
        }
    }
}