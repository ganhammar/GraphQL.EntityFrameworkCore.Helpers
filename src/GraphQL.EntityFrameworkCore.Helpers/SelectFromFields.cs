using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Language.AST;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public static class SelectFromFields
    {
        public static IModel Model { get; set; }

        public static IQueryable<TQuery> Select<TQuery>(this IQueryable<TQuery> query, IResolveFieldContext<object> context)
        {
            var entityType = typeof(TQuery);
            
            // The requested properties
            var properties = GetProperties(entityType, context.SubFields);
            
            // Input parameter, x
            var parameter = Expression.Parameter(entityType, "x");
            
            // New statement, new TQuery()
            var newEntity = Expression.New(entityType);

            // Set value, Field = x.Field
            var bindings = properties.Select(propertyType =>
                Expression.Bind(propertyType, Expression.Property(parameter, propertyType)));

            // Initialization, new TQuery { Field = x.Field, ... }
            var initializeEntity = Expression.MemberInit(newEntity, bindings);

            // Lambda expression, x => new TQuery { Field = x.Field, ... }
            var lambda = Expression.Lambda<Func<TQuery, TQuery>>(initializeEntity, parameter);

            return query.Select(lambda);
        }

        private static List<PropertyInfo> GetProperties(Type entityType, IDictionary<string, Field> fields)
        {
            var entity = Model.FindEntityType(entityType);
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

                        properties.AddRange(navigationProperty.ForeignKey.Properties.Select(x => x.PropertyInfo));
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

        private static IDictionary<string, Field> GetSelection(IDictionary<string, Field> fields)
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