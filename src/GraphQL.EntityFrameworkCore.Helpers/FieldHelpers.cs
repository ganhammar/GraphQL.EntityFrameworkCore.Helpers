using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldHelpers
    {
        private static Dictionary<string, Dictionary<EventStreamFieldType, string>> _properties = new Dictionary<string, Dictionary<EventStreamFieldType, string>>();

        public static void Map(Type entityType, EventStreamFieldType field, PropertyInfo property)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                _properties.Add(entityType.FullName, new Dictionary<EventStreamFieldType, string>());
            }

            if (_properties[entityType.FullName].ContainsKey(field) == false)
            {
                _properties[entityType.FullName].Add(field, property.Name);
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