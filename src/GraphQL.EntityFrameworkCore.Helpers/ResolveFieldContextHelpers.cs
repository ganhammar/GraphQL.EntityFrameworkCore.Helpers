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
        public static List<PropertyInfo> GetProperties(Type entityType, IDictionary<string, Field> fields, IModel model, IResolveFieldContext<object> context)
        {
            var entity = model.FindEntityType(entityType);
            var properties = new List<PropertyInfo>();
            var selection = GetSelection(fields, context);

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var propertyPath = FieldHelpers.GetPropertyPath(entityType, field.Value.Name);
                var property = entityType.GetProperty(propertyPath, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

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

        public static IDictionary<string, Field> GetSelection(IDictionary<string, Field> fields, IResolveFieldContext<object> context)
        {
            // The query is a connection, get the selection from node instead
            if (fields.Any(x => x.Key == "edges"))
            {
                return ToDictionary(fields["edges"]
                    .SelectionSet.Selections.Cast<Field>().First(x => x.Name == "node").SelectionSet.Selections, context);
            }

            return fields;
        }

        public static IDictionary<string, Field> ToDictionary(IList<ISelection> selections, IResolveFieldContext<object> context)
        {
            var result = new Dictionary<string, Field>();

            foreach (var selection in selections)
            {
                if (selection is Field childField && result.ContainsKey(childField.Name) == false)
                {
                    result.Add(childField.Name, childField);
                }
                else if (selection is FragmentSpread fragmentSpread)
                {
                    var fragmentSelection = context.Document.Fragments
                        .First(x => x.Name == fragmentSpread.Name);

                    result = result.Concat(ToDictionary(fragmentSelection.SelectionSet.Selections, context))
                        .Distinct()
                        .ToDictionary(x => x.Key, x => x.Value);
                }
            }

            return result;
        }
    }
}