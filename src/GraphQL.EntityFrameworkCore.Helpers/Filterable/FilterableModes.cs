using System.ComponentModel;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public enum FilterableModes
    {
        [Description("Applies the filter to any filtered properties, excluding data loaded fields")]
        Shallow,
        [Description("Applies the filter to any filtered properties, including data loaded fields")]
        Deep,
    }
}