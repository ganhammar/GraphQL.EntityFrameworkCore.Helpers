using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableInputField
    {
        public string Target { get; set; } = "All";
        public string Value { get; set; }
        public IEnumerable<FilterableInputField> Fields { get; set; }
        public FilterableFieldOperators Operator { get; set; } = FilterableFieldOperators.Or;
        public FilterableValueOperators ValueOperator { get; set; } = FilterableValueOperators.Like;
    }
}