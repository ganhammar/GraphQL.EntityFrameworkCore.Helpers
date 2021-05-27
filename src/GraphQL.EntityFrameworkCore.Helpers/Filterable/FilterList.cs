using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.EntityFrameworkCore.Helpers;
using GraphQL.Language.AST;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
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

        public static ValidationResult ValidateFilterInput(this IResolveFieldContext<object> context)
        {
            var filter = GetFilter(context);

            if (filter == default)
            {
                return new ValidationResult();
            }

            return filter.Validate(context);
        }

        public static FilterableInput GetFilter(IResolveFieldContext<object> context)
        {
            if (context == default)
            {
                return default;
            }

            var fields = GetOperationBranch(context.Operation.SelectionSet, context.FieldDefinition.Name, context);

            foreach (Field field in fields)
            {
                var filter = field.Arguments?.FirstOrDefault(x => x.Name.ToLower() == "filter");

                if (filter != default)
                {
                    var filterInputReference = (VariableReference)filter.Children.First(x => x.GetType() == typeof(VariableReference));
                    var input = GetFilterableInput(filterInputReference.Name, context);

                    if (context.FieldDefinition.Name == field.Name || input.Mode == FilterableModes.Deep)
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

            return (FilterableInput)variable.Value;
        }

        private static List<Field> GetOperationBranch(SelectionSet operation, string target, IResolveFieldContext<object> context)
        {
            var result = new List<Field>();

            foreach (var selection in operation.Selections)
            {
                if (selection is Field field)
                {
                    if (field.Name == target)
                    {
                        result.Add(field);
                    }
                    else
                    {
                        var children = GetOperationBranch(field.SelectionSet, target, context);

                        if (children.Any())
                        {
                            result.AddRange(children);
                            result.Add(field);
                        }
                    }
                }
                else if (selection is FragmentSpread fragmentSpread)
                {
                    
                }
            }

            return result;
        }

        private static IQueryable<TQuery> AddWheres<TQuery>(this IQueryable<TQuery> query, FilterableInput filter, IResolveFieldContext<object> context, IModel model)
        {
            var entityType = typeof(TQuery);
            var argument = Expression.Parameter(entityType);

            foreach (var expression in GetExpressions(argument, entityType, filter, context, model))
            {
                query = query.AddWhere(argument, entityType, expression);
            }

            return query;
        }

        public static List<Expression> GetExpressions(ParameterExpression argument, Type entityType, FilterableInput filter, IResolveFieldContext<object> context, IModel model)
        {
            var expressions = GetSelectionPaths(argument, filter.GetApplicableFilterFields(context), entityType, ResolveFieldContextHelpers.GetSelection(context.SubFields, context), model, context);
            var result = new List<Expression>();

            if (expressions == default)
            {
                return result;
            }

            if (expressions.ContainsKey(FilterableFieldOperators.Or) && expressions[FilterableFieldOperators.Or].Any())
            {
                Expression clause = default;

                expressions[FilterableFieldOperators.Or].ForEach(x =>
                {
                    clause = clause == default ? x : Expression.Or(clause, x);
                });

                result.Add(clause);
            }

            if (expressions.ContainsKey(FilterableFieldOperators.And) && expressions[FilterableFieldOperators.And].Any())
            {
                result.AddRange(expressions[FilterableFieldOperators.And]);
            }
            
            return result;
        }

        private static IQueryable<TQuery> AddWhere<TQuery>(this IQueryable<TQuery> query, ParameterExpression argument, Type entityType, Expression clause)
        {
            clause = Expression.Lambda(clause, argument);
            var method = QueryableExtensions.GetWhereMethod();

            MethodInfo genericMethod = method
                .MakeGenericMethod(entityType);

            return (IQueryable<TQuery>)genericMethod
                .Invoke(genericMethod, new object[] { query, clause });
        }

        private static Dictionary<FilterableFieldOperators, List<Expression>> GetSelectionPaths(
            Expression argument,
            IEnumerable<FilterableInputField> fields,
            Type entityType,
            IDictionary<string, Field> selection,
            IModel model,
            IResolveFieldContext<object> context)
        {
            var result = new Dictionary<FilterableFieldOperators, List<Expression>>();
            var entity = model.FindEntityType(entityType);
            var navigationProperties = entity.GetNavigations();

            foreach (var field in selection)
            {
                var propertyName = FieldHelpers.GetPropertyPath(entityType, field.Value.Name);
                var property = entityType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != default)
                {
                    var navigationProperty = navigationProperties.Where(x => x.Name == property.Name).FirstOrDefault();

                    if (navigationProperty != default)
                    {
                        var targetType = property.PropertyType;

                        if (typeof(IEnumerable).IsAssignableFrom(targetType))
                        {
                            targetType = targetType.GetGenericArguments().First();
                            var subArgument = Expression.Parameter(targetType);
                            var anyMethod = QueryableExtensions.GetAnyMethod()
                                .MakeGenericMethod(targetType);

                            result.Merge(GetSelectionPaths(
                                    subArgument,
                                    GetFilterFields(field.Value.Name, fields),
                                    targetType,
                                    ResolveFieldContextHelpers.ToDictionary(field.Value.SelectionSet.Selections, context),
                                    model,
                                    context),
                                x => Expression.Call(
                                    anyMethod,
                                    Expression.MakeMemberAccess(argument, property),
                                    Expression.Lambda(x, subArgument)));
                        }
                        else
                        {
                            result.Merge(GetSelectionPaths(
                                Expression.Property(argument, property.Name),
                                GetFilterFields(field.Value.Name, fields),
                                targetType,
                                ResolveFieldContextHelpers.ToDictionary(field.Value.SelectionSet.Selections, context),
                                model,
                                context));
                        }
                    }
                }
            }

            result.Merge(GetExpression(argument, fields, entityType, selection, model, context));

            return result;
        }

        private static Dictionary<FilterableFieldOperators, List<Expression>> Merge(
            this Dictionary<FilterableFieldOperators, List<Expression>> main,
            Dictionary<FilterableFieldOperators, List<Expression>> second,
            Func<Expression, Expression> mergeAction = default)
        {
            if (second == default)
            {
                return main;
            }

            foreach(KeyValuePair<FilterableFieldOperators, List<Expression>> entry in second)
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

        private static IEnumerable<FilterableInputField> GetFilterValueForField(string fieldName, IEnumerable<FilterableInputField> fields)
        {
            var matches = fields.Where(x => x.Target.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

            return matches.Any() ? matches : fields.Where(x => x.Target.Equals("All", StringComparison.InvariantCultureIgnoreCase));
        }

        private static Dictionary<FilterableFieldOperators, List<Expression>> GetExpression(
            Expression argument,
            IEnumerable<FilterableInputField> fields,
            Type entityType,
            IDictionary<string, Field> selection,
            IModel model,
            IResolveFieldContext<object> context)
        {
            var result = new Dictionary<FilterableFieldOperators, List<Expression>>();
            result.Add(FilterableFieldOperators.Or, new List<Expression>());
            result.Add(FilterableFieldOperators.And, new List<Expression>());

            var convertToStringMethod = typeof(object).GetMethod("ToString");
            var concatMethod = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });
            var equalMethod = typeof(Expression).GetMethod(nameof(string.Equals), new[] { typeof(Expression), typeof(Expression) });

            Expression orClause = default;
            ResolveFieldContextHelpers
                .GetProperties(entityType, selection, model, context)
                .Where(FilterableHelpers.IsFilterable)
                .ToList()
                .ForEach(field =>
                {
                    var filters = GetFilterValueForField(FieldHelpers.GetSchemaName(entityType, field.Name), fields);

                    if (filters.Any() == false)
                    {
                        return;
                    }

                    foreach(var filter in filters)
                    {
                        Expression property = Expression.MakeMemberAccess(argument, field);

                        if (field.PropertyType != typeof(string))
                        {
                            property = Expression.Call(property, convertToStringMethod);
                        }

                        Expression compareToExpression = default;
                        var isEqual = true;

                        switch(filter.ValueOperator)
                        {
                            case FilterableValueOperators.Like: case FilterableValueOperators.Notlike:
                                compareToExpression = Expression.Constant($"%{filter.Value}%");
                                isEqual = filter.ValueOperator == FilterableValueOperators.Like;
                                break;
                            case FilterableValueOperators.Equal: case FilterableValueOperators.Notequal:
                                compareToExpression = Expression.Constant(filter.Value);
                                isEqual = filter.ValueOperator == FilterableValueOperators.Equal;
                                break;
                        }

                        property = Expression.Equal(Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), property, compareToExpression), Expression.Constant(isEqual));

                        if (filter.Operator == FilterableFieldOperators.Or)
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
                            result[FilterableFieldOperators.And].Add(property);
                        }
                    }
                });
            
            if (orClause != default)
            {
                result[FilterableFieldOperators.Or].Add(orClause);
            }

            return result;
        }
    }
}