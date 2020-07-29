using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableOperandsGraphType : EnumerationGraphType<FilterableOperands>
    {
        public FilterableOperandsGraphType()
        {
            Name = "FilterOperand";
        }
    }
}