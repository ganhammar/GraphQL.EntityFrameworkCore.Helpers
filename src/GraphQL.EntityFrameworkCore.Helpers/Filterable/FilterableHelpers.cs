using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FilterableHelpers
    {
        private static ConcurrentDictionary<string, string> _properties = new ConcurrentDictionary<string, string>();

        public static void Add(Type entityType, PropertyInfo property)
        {
            _properties.TryAdd(entityType.FullName, property.Name);
        }

        public static void Add(Type entityType, string schemaName)
        {
            _properties.TryAdd(entityType.FullName, schemaName);
        }
        
        public static bool IsFilterable(PropertyInfo property)
        {
            return Attribute.IsDefined(property, typeof(FilterableAttribute)) ||
                _properties.Any(x => x.Key == property.ReflectedType.FullName && x.Value == property.Name);
        }
    }
}