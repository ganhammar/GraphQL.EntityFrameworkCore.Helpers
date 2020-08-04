using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableValueOperatorsGraphType : EnumerationGraphType<FilterableValueOperators>
    {
        public FilterableValueOperatorsGraphType()
        {
            Name = "FilterValueOperator";
        }
    }
}