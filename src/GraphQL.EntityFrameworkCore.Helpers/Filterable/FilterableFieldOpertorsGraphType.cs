using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableFieldOperatorsGraphType : EnumerationGraphType<FilterableFieldOperators>
    {
        public FilterableFieldOperatorsGraphType()
        {
            Name = "FilterFieldOperator";
        }
    }
}