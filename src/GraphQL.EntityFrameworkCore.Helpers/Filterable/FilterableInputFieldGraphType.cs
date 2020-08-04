using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableInputFieldGraphType : InputObjectGraphType<FilterableInputField>
    {
        public FilterableInputFieldGraphType()
        {
            Name = "FilterField";

            Field(x => x.Target, nullable: true).Description("The target field to filter by, All (default) to apply to all included fields");
            Field(x => x.Value, nullable: true).Description("String to filter by (ignored when targeting data loaded property)");
            Field(x => x.Fields, type: typeof(ListGraphType<FilterableInputFieldGraphType>)).Description("Used when filtering on a data loaded property");
            Field(x => x.Operator, type: typeof(FilterableOperatorsGraphType)).Description("The operator to use for the filter (Or is default, ignored when targeting data loaded property)");
        }
    }
}