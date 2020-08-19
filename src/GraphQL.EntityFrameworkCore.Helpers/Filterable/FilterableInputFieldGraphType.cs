using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableInputFieldGraphType : InputObjectGraphType<FilterableInputField>
    {
        public FilterableInputFieldGraphType()
        {
            Name = "FilterField";

            Field(x => x.Target, nullable: true)
                .Description("The target field to filter by, All (default) to apply to all included fields");
            Field(x => x.Value, nullable: true)
                .Description("String to filter by (ignored when targeting data loaded property)");
            Field(x => x.Fields, type: typeof(ListGraphType<FilterableInputFieldGraphType>))
                .Description("Used when filtering on a data loaded property");
            Field(x => x.Operator, type: typeof(FilterableFieldOperatorsGraphType))
                .Description("The operator used to compare the result of the filtered field against others (Or is default, ignored when targeting data loaded property)");
            Field(x => x.ValueOperator, type: typeof(FilterableValueOperatorsGraphType))
                .Description("The operator to use to compare the target with the value (Like is default, ignored when targeting data loaded property)");
        }
    }
}