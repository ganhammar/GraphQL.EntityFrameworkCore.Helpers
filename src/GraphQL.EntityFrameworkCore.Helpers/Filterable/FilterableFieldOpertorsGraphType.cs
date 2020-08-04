using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableFieldOperatorsGraphType : EnumerationGraphType<FilterableFieldOperators>
    {
        public FilterableFieldOperatorsGraphType()
        {
            Name = "FilterFieldOperator";
        }
    }
}