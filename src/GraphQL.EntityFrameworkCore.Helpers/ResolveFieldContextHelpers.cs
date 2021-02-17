using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphQL.Language.AST;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class ResolveFieldContextHelpers
    {
        public static List<PropertyInfo> GetProperties(Type entityType, IDictionary<string, Field> fields, IModel model)
        {
            var entity = model.FindEntityType(entityType);
            var properties = new List<PropertyInfo>();
            var selection = GetSelection(fields);

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var propertyPath = FieldHelpers.GetPropertyPath(entityType, field.Value.Name);
                var property = entityType.GetProperty(propertyPath.First(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null && (property.PropertyType.IsClass == false ||
                    property.PropertyType.FullName.StartsWith("System.")) &&
                    (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) == false ||
                    property.PropertyType == typeof(string)))
                {
                    properties.Add(property);
                }
            }

            // Include primary key
            foreach (var property in entity.FindPrimaryKey().Properties)
            {
                if (properties.Any(y => y.Name == property.PropertyInfo.Name) == false)
                {
                    properties.Add(property.PropertyInfo);
                }
            }

            return properties;
        }

        public static IDictionary<string, Field> GetSelection(IDictionary<string, Field> fields)
        {
            // The query is a connection, get the selection from node instead
            if (fields.Any(x => x.Key == "edges"))
            {
                return fields["edges"]
                    .SelectionSet.Selections.Cast<Field>().First(x => x.Name == "node")
                    .SelectionSet.Selections.Cast<Field>().ToDictionary(x => x.Name, x => x);
            }

            return fields;
        }
    }
}