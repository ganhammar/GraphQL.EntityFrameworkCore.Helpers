using System.ComponentModel;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public enum FilterableValueOperators
    {
        [Description("Target(s) contains value")]
        Like,
        [Description("Target(s) doesn't contain value")]
        Notlike,
        [Description("Target(s) equals value")]
        Equal,
        [Description("Target(s) not equals value")]
        Notequal,
    }
}