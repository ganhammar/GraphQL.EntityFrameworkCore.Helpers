using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FilterableHelpers
    {
        private static ConcurrentDictionary<string, List<string>> _properties = new ConcurrentDictionary<string, List<string>>();

        private static void EnsureCreated(Type entityType)
        {
            if (_properties.Any(x => x.Key == entityType.FullName) == false)
            {
                _properties.TryAdd(entityType.FullName, new List<string>());
            }
        }

        public static void Add(Type entityType, PropertyInfo property)
        {
            EnsureCreated(entityType);
            
            _properties[entityType.FullName].Add(property.Name);
        }

        public static void Add(Type entityType, string schemaName)
        {
            EnsureCreated(entityType);

            _properties[entityType.FullName].Add(schemaName);
        }
        
        public static bool IsFilterable(PropertyInfo property)
        {
            return Attribute.IsDefined(property, typeof(FilterableAttribute)) ||
                _properties.Any(x => x.Key == property.ReflectedType.FullName && x.Value.Any(y => y == property.Name));
        }
    }
}