using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableModesGraphType : EnumerationGraphType<FilterableModes>
    {
        public FilterableModesGraphType()
        {
            Name = "FilterMode";
        }
    }
}