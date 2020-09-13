namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ConnectionInput<T> : IConnectionInput<T>
    {
        public string After { get; set; }
        public string Before { get; set; }
        public int First { get; set; }
        public bool IsAsc { get; set; }
        public string[] OrderBy { get; set; }
        public string Filter { get; set; }
        public IResolveFieldContext<object> Context { get; set; }
    }
}