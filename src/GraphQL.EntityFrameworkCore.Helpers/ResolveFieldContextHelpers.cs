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
            var navigationProperties = entity.GetNavigations();

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var property = entityType.GetProperty(field.Value.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null && navigationProperties.Any(x => x.Name == property.Name) == false)
                {
                    properties.Add(property);
                }
            }

            // Include foreign key value(s) and expect data loader or is data loaded
            foreach (var navigationProperty in navigationProperties)
            {
                if (typeof(IEnumerable).IsAssignableFrom(navigationProperty.PropertyInfo.PropertyType))
                {
                    continue;
                }

                var foreignKeyProperties = navigationProperty.ForeignKey.Properties
                    .Where(x => x.PropertyInfo != null).Select(x => x.PropertyInfo);

                if (foreignKeyProperties.Any())
                {
                    properties.AddRange(foreignKeyProperties);
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