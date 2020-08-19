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
        private static ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, List<string>>> _properties = new ConcurrentDictionary<string, ConcurrentDictionary<EventStreamFieldType, List<string>>>();

        public static void Map(Type entityType, EventStreamFieldType field, PropertyInfo property)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                _properties.TryAdd(entityType.FullName, new ConcurrentDictionary<EventStreamFieldType, List<string>>());
            }

            if (_properties[entityType.FullName].ContainsKey(field) == false)
            {
                _properties[entityType.FullName].TryAdd(field, new List<string> { property.Name });
            }
            else if (_properties[entityType.FullName][field].Any(x => x == property.Name) == false)
            {
                _properties[entityType.FullName][field].Add(property.Name);
            }
        }
        
        public static List<string> GetPropertyPath(Type entityType, string schemaName)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                return new List<string> { schemaName };
            }

            var property = _properties[entityType.FullName]
                .Where(x => x.Key.Name.Equals(schemaName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => (KeyValuePair<EventStreamFieldType, List<string>>?)x)
                .FirstOrDefault();

            return property?.Value ?? new List<string> { schemaName };
        }

        public static string GetSchemaName(Type entityType, string propertyName)
        {
            if (_properties.ContainsKey(entityType.FullName) == false)
            {
                return propertyName;
            }

            var property = _properties[entityType.FullName]
                .Where(x => x.Value.First().Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => (KeyValuePair<EventStreamFieldType, List<string>>?)x)
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