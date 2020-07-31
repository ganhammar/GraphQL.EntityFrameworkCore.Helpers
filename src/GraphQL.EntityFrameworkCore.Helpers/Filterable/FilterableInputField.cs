using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public class FilterableInputField
    {
        public string Target { get; set; } = "All";
        public string Value { get; set; }
        public IEnumerable<FilterableInputField> Fields { get; set; }
        public FilterableOperands Operand { get; set; } = FilterableOperands.Or;
    }
}