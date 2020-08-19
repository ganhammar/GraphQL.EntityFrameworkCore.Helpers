using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableValueOperatorsGraphType : EnumerationGraphType<FilterableValueOperators>
    {
        public FilterableValueOperatorsGraphType()
        {
            Name = "FilterValueOperator";
        }
    }
}