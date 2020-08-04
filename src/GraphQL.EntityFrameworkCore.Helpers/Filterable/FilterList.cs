using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.EntityFrameworkCore.Helpers.Connection;
using GraphQL.Language.AST;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers.Filterable
{
    public static class FilterList
    {
        public static IQueryable<TQuery> Filter<TQuery>(this IQueryable<TQuery> query, IResolveFieldContext<object> context, IModel model)
        {
            var filter = GetFilter(context);

            if (filter == default)
            {
                return query;
            }

            var validationResult = filter.Validate(context);
            if (validationResult.IsValid == false)
            {
                throw new Exception(validationResult.Failures.First().Message);
            }

            return AddWheres(query, filter, context, model);
        }

        private static FilterableInput GetFilter(IResolveFieldContext<object> context)
        {
            if (context == default)
            {
                return default;
            }

            var fields = GetOperationBranch(context.Operation.SelectionSet, context.FieldName);

            foreach (Field field in fields)
            {
                var filter = field.Arguments?.FirstOrDefault(x => x.Name.ToLower() == "filter");

                if (filter != default)
                {
                    var filterInputReference = (VariableReference)filter.Children.First(x => x.GetType() == typeof(VariableReference));
                    var input = GetFilterableInput(filterInputReference.Name, context);

                    if (context.FieldName == field.Name || input.Mode == FilterableModes.Deep)
                    {
                        return input;
                    }
                }
            }

            return default;
        }

        private static FilterableInput GetFilterableInput(string inputName, IResolveFieldContext<object> context)
        {
            var variable = context.Variables.First(x => x.Name == inputName);
            var arguments = variable.Value as IDictionary<string, object>;

            return (FilterableInput)arguments.ToObject(typeof(FilterableInput));
        }

        private static List<Field> GetOperationBranch(SelectionSet operation, string target)
        {
            var result = new List<Field>();

            foreach (Field selection in operation.Selections)
            {
                if (selection.Name == target)
                {
                    result.Add(selection);
                }
                else
                {
                    var children = GetOperationBranch(selection.SelectionSet, target);

                    if (children.Any())
                    {
                        result.AddRange(children);
                        result.Add(selection);
                    }
                }
            }

            return result;
        }

        private static IQueryable<TQuery> AddWheres<TQuery>(this IQueryable<TQuery> query, FilterableInput filter, IResolveFieldContext<object> context, IModel model)
        {
            var entityType = typeof(TQuery);
            var argument = Expression.Parameter(entityType);
            var expressions = GetSelectionPaths(argument, filter.GetApplicableFilterFields(context), entityType, context.SubFields, model, query);

            if (expressions == default)
            {
                return query;
            }

            if (expressions.ContainsKey(FilterableOperators.Or) && expressions[FilterableOperators.Or].Any())
            {
                Expression clause = default;

                expressions[FilterableOperators.Or].ForEach(x =>
                {
                    clause = clause == default ? x : Expression.Or(clause, x);
                });

                query = query.AddWhere(argument, entityType, clause);
            }

            if (expressions.ContainsKey(FilterableOperators.And) && expressions[FilterableOperators.And].Any())
            {
                expressions[FilterableOperators.And].ForEach(clause =>
                {
                    query = query.AddWhere(argument, entityType, clause);
                });
            }

            return query;
        }

        private static IQueryable<TQuery> AddWhere<TQuery>(this IQueryable<TQuery> query, ParameterExpression argument, Type entityType, Expression clause)
        {
            clause = Expression.Lambda(clause, argument);
            var method = GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType);

            return (IQueryable<TQuery>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static Dictionary<FilterableOperators, List<Expression>> GetSelectionPaths<TQuery>(Expression argument, IEnumerable<FilterableInputField> fields, Type entityType, IDictionary<string, Field> selection, IModel model, IQueryable<TQuery> query)
        {
            var result = new Dictionary<FilterableOperators, List<Expression>>();
            var entity = model.FindEntityType(entityType);
            var navigationProperties = entity.GetNavigations();

            foreach (var field in selection)
            {
                // Ignore case, camelCase vs PascalCase
                var property = entityType.GetProperty(FieldHelpers.GetPropertyName(entityType, field.Value.Name), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    var navigationProperty = navigationProperties.Where(x => x.Name == property.Name).FirstOrDefault();

                    if (navigationProperty != default)
                    {
                        var targetType = property.PropertyType;

                        if (typeof(IEnumerable).IsAssignableFrom(targetType))
                        {
                            targetType = targetType.GetGenericArguments().First();
                            var subArgument = Expression.Parameter(targetType);
                            var anyMethod = GetAnyMethod().MakeGenericMethod(targetType);

                            result.Merge(GetSelectionPaths(
                                    subArgument,
                                    GetFilterFields(field.Value.Name, fields),
                                    targetType,
                                    ToDictionary(field),
                                    model,
                                    query
                                ),
                                x => Expression.Call(
                                    anyMethod,
                                    Expression.MakeMemberAccess(argument, property),
                                    Expression.Lambda(x, subArgument)
                                )
                            );
                        }
                        else
                        {
                            result.Merge(GetSelectionPaths(
                                Expression.Property(argument, property.Name),
                                GetFilterFields(field.Value.Name, fields),
                                targetType,
                                ToDictionary(field),
                                model,
                                query
                            ));
                        }
                    }
                }
            }

            result.Merge(GetExpression(argument, fields, entityType, selection, model));

            return result;
        }

        private static Dictionary<FilterableOperators, List<Expression>> Merge(this Dictionary<FilterableOperators, List<Expression>> main, Dictionary<FilterableOperators, List<Expression>> second, Func<Expression, Expression> mergeAction = default)
        {
            if (second == default)
            {
                return main;
            }

            foreach(KeyValuePair<FilterableOperators, List<Expression>> entry in second)
            {
                if (main.ContainsKey(entry.Key) == false)
                {
                    main.Add(entry.Key, new List<Expression>());
                }

                if (mergeAction == default)
                {
                    main[entry.Key].AddRange(entry.Value);
                }
                else
                {
                    entry.Value.ForEach(x =>
                    {
                        main[entry.Key].Add(mergeAction(x));
                    });
                }
            }

            return main;
        }

        private static IEnumerable<FilterableInputField> GetFilterFields(string fieldName, IEnumerable<FilterableInputField> fields)
        {
            var subFilter = fields.FirstOrDefault(x => x.Target.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

            if (subFilter == default)
            {
                return fields.Where(x => x.Target.Equals("All", StringComparison.InvariantCultureIgnoreCase));
            }
            
            return subFilter.Fields;
        }

        private static FilterableInputField GetFilterValueForField(string fieldName, IEnumerable<FilterableInputField> fields)
            => fields.FirstOrDefault(x => x.Target.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) 
                ?? fields.FirstOrDefault(x => x.Target.Equals("All", StringComparison.InvariantCultureIgnoreCase));

        private static Dictionary<string, Field> ToDictionary(KeyValuePair<string, Field> field)
            => field.Value.SelectionSet.Selections.ToDictionary(x => (x as Field).Name, x => x as Field);

        private static Dictionary<FilterableOperators, List<Expression>> GetExpression(Expression argument, IEnumerable<FilterableInputField> fields, Type entityType, IDictionary<string, Field> selection, IModel model)
        {
            var result = new Dictionary<FilterableOperators, List<Expression>>();
            result.Add(FilterableOperators.Or, new List<Expression>());
            result.Add(FilterableOperators.And, new List<Expression>());

            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

            Expression orClause = default;
            ResolveFieldContextHelpers
                .GetProperties(entityType, selection, model)
                .Where(FilterableHelpers.IsFilterable)
                .ToList()
                .ForEach(field =>
                {
                    var filter = GetFilterValueForField(FieldHelpers.GetSchemaName(entityType, field.Name), fields);

                    if (filter?.Value == default)
                    {
                        return;
                    }

                    var compareToExpression = Expression.Constant($"%{filter.Value}%");
                    Expression property = Expression.MakeMemberAccess(argument, field);

                    if (field.PropertyType != typeof(string))
                    {
                        property = Expression.Call(property, convertToStringMethod);
                    }

                    property = Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), property, compareToExpression);

                    if (filter.Operator == FilterableOperators.Or)
                    {
                        if (orClause != null)
                        {
                            orClause = Expression.Or(orClause, property);
                        }
                        else
                        {
                            orClause = property;
                        }
                    }
                    else
                    {
                        result[FilterableOperators.And].Add(property);
                    }
                });
            
            if (orClause != default)
            {
                result[FilterableOperators.Or].Add(orClause);
            }

            return result;
        }

        private static MethodInfo GetWhereMethod() => typeof(Queryable)
            .GetMethods()
            .Where(m => m.Name == "Where" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();

        private static MethodInfo GetAnyMethod() => typeof(Enumerable)
            .GetMethods()
            .Where(m => m.Name == "Any" && m.IsGenericMethodDefinition)
            .Where(m => m.GetParameters().ToList().Count == 2)
            .First();
    }
}