using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableOperatorsGraphType : EnumerationGraphType<FilterableOperators>
    {
        public FilterableOperatorsGraphType()
        {
            Name = "FilterOperator";
        }
    }
}