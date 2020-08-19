using System;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class MapsFromAttribute : Attribute
    {
        public MapsFromAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; set; }
    }
}