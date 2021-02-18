using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Types;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class FieldHelpers
    {
        private static ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, string>> _properties = new ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, string>>();

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
        }
        
        public static string GetPropertyPath(Type entityType, string schemaName)
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

        public static string GetPropertyPath(Type entityType, EventStreamFieldType field)
            => _properties[entityType.FullName][field];

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

        public static PropertyInfo GetPropertyInfo<TSourceType, TProperty>(Expression<Func<TSourceType, TProperty>> accessor)
        {
            var type = typeof(TSourceType);

            var member = accessor.Body as MemberExpression;
            if (member == null) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    accessor.ToString()));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    accessor.ToString()));
            }

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType)) {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    accessor.ToString(),
                    type));
            }

            return propInfo;
        }
    }
}