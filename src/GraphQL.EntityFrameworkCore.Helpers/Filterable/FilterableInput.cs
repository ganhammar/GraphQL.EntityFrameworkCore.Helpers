using System.Collections.Generic;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FilterableInput
    {
        public FilterableModes Mode { get; set; } = FilterableModes.Shallow;
        public IEnumerable<FilterableInputField> Fields { get; set; }
    }
}