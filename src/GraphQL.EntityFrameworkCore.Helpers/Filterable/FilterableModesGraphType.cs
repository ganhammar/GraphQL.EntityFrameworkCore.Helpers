using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableModesGraphType : EnumerationGraphType<FilterableModes>
    {
        public FilterableModesGraphType()
        {
            Name = "FilterMode";
        }
    }
}