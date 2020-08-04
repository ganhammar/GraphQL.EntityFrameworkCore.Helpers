using System.ComponentModel;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public enum FilterableOperators
    {
        [Description("Treated as separate where clauses")]
        And,
        [Description("All Or fields are joined in one where clause")]
        Or,
    }
}