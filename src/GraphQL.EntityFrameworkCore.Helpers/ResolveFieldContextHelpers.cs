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
            var navigationProperties = entity.GetNavigations();
            var properties = new List<PropertyInfo>();
            var selection = GetSelection(fields);

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var property = entityType.GetProperty(field.Value.Name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    // If navigation property, include foreign key value(s) and expect data loader
                    if (navigationProperties.Any(x => x.Name == property.Name))
                    {
                        if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                        {
                            continue;
                        }

                        var navigationProperty = navigationProperties.Where(x => x.Name == property.Name).First();
                        var foreignKeyProperties = navigationProperty.ForeignKey.Properties
                            .Where(x => x.PropertyInfo != null).Select(x => x.PropertyInfo);

                        if (foreignKeyProperties.Any())
                        {
                            properties.AddRange(foreignKeyProperties);
                        }
                    }
                    else
                    {
                        properties.Add(property);
                    }
                }
            }

            // Include primary key
            entity.FindPrimaryKey().Properties.ToList().ForEach(x =>
            {
                if (properties.Any(y => y.Name == x.PropertyInfo.Name) == false)
                {
                    properties.Add(x.PropertyInfo);
                }
            });

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