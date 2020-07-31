using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldHelpers
    {
        public static ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, string>> _properties = new ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, string>>();

        public static void Map(Type entityType, EventStreamFieldType field, PropertyInfo property)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                _properties.TryAdd(entityType.FullName, new ConcurrentDictionary<EventStreamFieldType, string>());
            }

            if (_properties[entityType.FullName].ContainsKey(field) == false)
            {
                _properties[entityType.FullName].TryAdd(field, property.Name);
            }
            else
            {
                _properties[entityType.FullName][field] = property.Name;
            }
        }
        
        public static string GetPropertyName(Type entityType, string schemaName)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                return schemaName;
            }

            var property = _properties[entityType.FullName]
                .Where(x => x.Key.Name.Equals(schemaName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => (KeyValuePair<EventStreamFieldType, string>?)x)
                .FirstOrDefault();

            return property?.Value ?? schemaName;
        }

        public static string GetSchemaName(Type entityType, string propertyName)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                return propertyName;
            }

            var property = _properties[entityType.FullName]
                .Where(x => x.Value.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => (KeyValuePair<EventStreamFieldType, string>?)x)
                .FirstOrDefault();

            return property?.Key.Name ?? propertyName;
        }
    }
}