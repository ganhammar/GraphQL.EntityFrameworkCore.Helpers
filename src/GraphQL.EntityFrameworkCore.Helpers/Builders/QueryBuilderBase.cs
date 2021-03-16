using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public partial class QueryBuilderBase<TReturnType, TContextType>
        where TContextType : IResolveFieldContext
    {
        protected PropertyInfo _propertyToInclude;
        protected Type _dbContextType;
        protected Func<TContextType, ValidationResult> ValidationAction { get; set; }
        protected Func<TContextType, Task<ValidationResult>> AsyncValidationAction { get; set; }
        protected Func<IResolveFieldContext<object>, Expression<Func<TReturnType, bool>>> BusinuessCheck { get; set; }

        private async Task<bool> ValidateBusiness(TContextType context, IModel model)
        {
            if (ValidationAction != default)
            {
                return AddErrors(context, ValidationAction(context));
            }
            else if (AsyncValidationAction != default)
            {
                return AddErrors(context, await AsyncValidationAction(context));
            }

            return true;
        }

        protected async Task<bool> IsValid(TContextType context, IModel model)
        {
            var isValid = await ValidateBusiness(context, model);

            if (isValid == false || ValidateFilterInput(context) == false)
            {
                return false;
            }

            return true;
        }

        protected bool ValidateConnectionInput(TContextType context, IConnectionInput<TReturnType> input, IModel model)
        {
            return AddErrors(context, input.Validate<TReturnType, TReturnType>(model));
        }

        protected bool ValidateFilterInput(TContextType context)
        {
            return AddErrors((TContextType)context,
                ((IResolveFieldContext<object>)context).ValidateFilterInput());
        }

        private static bool AddErrors(TContextType context, ValidationResult result)
        {
            if (result.IsValid == false || result.Failures.Any() == true)
            {
                if (result.Failures.Any())
                {
                    result.Failures.ToList().ForEach(x => 
                    {
                        var data = new Dictionary<string, string>();

                        if (x.FieldName != default)
                        {
                            data.Add("FieldName", x.FieldName);
                        }

                        context.Errors.Add(new ExecutionError(x.Message, data));
                    });
                }
                else
                {
                    context.Errors.Add(new ExecutionError("Query is not valid"));
                }
            }

            return result.IsValid;
        }

        protected IQueryable<TReturnType> ApplyBusinessCheck(IQueryable<TReturnType> query, IResolveFieldContext<object> context)
        {
            if (BusinuessCheck == default)
            {
                return query;
            }

            return query.Where(BusinuessCheck(context));
        }

        protected Dictionary<IProperty, object> GetKeyValuePairs(Type sourceType, IModel model, IResolveFieldContext<object> context)
        {
            var entity = model.FindEntityType(sourceType);
            var primaryKeys = entity.FindPrimaryKey().Properties;

            var keyValues = new Dictionary<IProperty, object>();

            foreach (var primaryKey in primaryKeys)
            {
                keyValues.Add(primaryKey, sourceType
                    .GetProperty(primaryKey.Name)
                    .GetValue(context.Source));
            }

            return keyValues;
        }

        protected Dictionary<IProperty, List<object>> MapProperties(
            IEnumerable<Dictionary<IProperty, object>> keyProperties)
        {
            var mappedProperties = new Dictionary<IProperty, List<object>>();

            foreach (var keyProperty in keyProperties)
            {
                foreach (var property in keyProperty)
                {
                    if (mappedProperties.ContainsKey(property.Key) == false)
                    {
                        mappedProperties.Add(property.Key, new List<object>());
                    }

                    mappedProperties[property.Key].Add(property.Value);
                }
            }

            return mappedProperties;
        }

        protected IQueryable<TSourceType> FilterBasedOnKeyProperties<TSourceType>(
            IQueryable<TSourceType> query,
            Dictionary<IProperty, List<object>> mappedKeyProperties,
            ParameterExpression argument,
            MethodInfo whereMethod)
        {
            foreach (var property in mappedKeyProperties)
            {
                var castMethod = QueryableExtensions.GetCastMethod()
                    .MakeGenericMethod(property.Key.PropertyInfo.PropertyType);
                var castedSourceProperties = castMethod.Invoke(castMethod, new object[] { property.Value });
                var properties = Expression.Constant(castedSourceProperties);

                var propertyAccess = Expression.MakeMemberAccess(argument, property.Key.PropertyInfo);

                var check = Expression.Call(typeof(Enumerable), "Contains",
                    new[] { property.Key.PropertyInfo.PropertyType }, properties, propertyAccess);
                var lambda = Expression.Lambda(check, argument);

                query = (IQueryable<TSourceType>)whereMethod
                    .Invoke(whereMethod, new object[] { query, lambda });
            }

            return query;
        }
    }
}