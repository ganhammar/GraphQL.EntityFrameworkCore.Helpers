using System.Collections.Generic;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers.Connection
{
    public interface IConnectionInput<T>
    {
        string After { get; set; }
        string Before { get; set; }
        int First { get; set; }
        bool IsAsc { get; set; }
        string[] OrderBy { get; set; }
        string Filter { get; set; }
        IResolveFieldContext<object> Context { get; set; }
    }
}