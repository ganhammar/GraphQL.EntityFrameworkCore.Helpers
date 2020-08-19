using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableInputGraphType : InputObjectGraphType<FilterableInput>
    {
        public FilterableInputGraphType()
        {
            Name = "FilterInput";

            Field(x => x.Mode, type: typeof(FilterableModesGraphType));
            Field(x => x.Fields, type: typeof(NonNullGraphType<ListGraphType<FilterableInputFieldGraphType>>));
        }
    }
}