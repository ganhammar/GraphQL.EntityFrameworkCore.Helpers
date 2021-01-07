using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class BatchQueryBuilder<TSourceType, TReturnType> : QueryBuilderBase<TReturnType, IResolveFieldContext<object>>
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly PropertyInfo _propertyToInclude;
        private readonly Type _dbContextType;

        public BatchQueryBuilder(
            FieldBuilder<TSourceType, TReturnType> field,
            Expression<Func<TSourceType, TReturnType>> propertyToInclude,
            Type dbContextType = null)
        {
            _field = field;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, TReturnType>(propertyToInclude);
            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public BatchQueryBuilder<TSourceType, TReturnType> Apply(
            Func<IQueryable<TReturnType>, IResolveFieldContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            BusinessLogic = businessLogic;

            return this;
        }

        public BatchQueryBuilder<TSourceType, TReturnType> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public BatchQueryBuilder<TSourceType, TReturnType> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        public void ResolveAsync() => _field.ResolveAsync(typedContext =>
        {
            var context = (IResolveFieldContext<object>)typedContext;

            if (context.RequestServices == default)
            {
                throw new Exception("ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on");
            }

            var dbContext = (DbContext)context.RequestServices.GetRequiredService(_dbContextType);
            var model = dbContext.Model;

            var sourceType = typeof(TSourceType);
            var targetType = typeof(TReturnType);
            var entity = model.FindEntityType(sourceType);
            var primaryKeys = entity.FindPrimaryKey().Properties;

            var keyValues = new Dictionary<IProperty, object>();

            foreach (var primaryKey in primaryKeys)
            {
                keyValues.Add(primaryKey, sourceType
                    .GetProperty(primaryKey.Name)
                    .GetValue(context.Source));
            }

            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            var dataLoaderContextAccessor = context.RequestServices.GetRequiredService<IDataLoaderContextAccessor>();
            var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<Dictionary<IProperty, object>, TReturnType>(
                loaderName,
                async (keyProperties) =>
                {
                    var isValid = await ValidateBusiness(context, dbContext.Model);

                    if (!isValid && ValidateFilterInput(context))
                    {
                        return default;
                    }

                    MethodInfo whereMethod = QueryableExtensions
                        .GetWhereMethod()
                        .MakeGenericMethod(sourceType);
                    var query = (IQueryable<TSourceType>)QueryableExtensions.GetSetMethod<TSourceType>()
                        .MakeGenericMethod(sourceType)
                        .Invoke(dbContext, null);
                    var argument = Expression.Parameter(sourceType);

                    var nestedQuery = (IQueryable<TReturnType>)QueryableExtensions.GetSetMethod<TReturnType>()
                        .MakeGenericMethod(targetType)
                        .Invoke(dbContext, null);
                    
                    nestedQuery = nestedQuery.Filter(context, model);
                    nestedQuery = ApplyBusinessLogic(nestedQuery, context);

                    var targetArgument = Expression.Parameter(targetType);
                    var filters = FilterList.GetFilter(context);
                    var expressions = filters != default
                        ? FilterList.GetExpressions(targetArgument, targetType, filters, context, model)
                        : new List<Expression>();

                    if (expressions.Any())
                    {
                        var existingWhere = (MethodCallExpression)nestedQuery.Expression;

                        var targetDbContextProperty = _dbContextType
                            .GetProperties()
                            .Where(x => x.PropertyType.IsGenericType)
                            .Where(x => x.PropertyType.GenericTypeArguments.Contains(targetType))
                            .First();

                        Expression dbContextAccess = Expression.MakeMemberAccess(Expression.Constant(dbContext), targetDbContextProperty);

                        foreach (var expression in expressions)
                        {
                            var lambda = Expression.Lambda<Func<TReturnType, bool>>(
                                expression, new ParameterExpression[] { targetArgument });
                            dbContextAccess = Expression.Call(typeof(Queryable), "Where",
                                new[] { _propertyToInclude.PropertyType }, dbContextAccess, lambda);
                        }

                        var targetPropertyAccess = Expression.MakeMemberAccess(argument, _propertyToInclude);
                        var nestedCheck = Expression.Call(typeof(Queryable), "Contains",
                            new[] { _propertyToInclude.PropertyType }, dbContextAccess, targetPropertyAccess);
                        var nestedLambda = Expression.Lambda(nestedCheck, argument);

                        query = (IQueryable<TSourceType>)whereMethod
                            .Invoke(whereMethod, new object[] { query, nestedLambda });
                    }

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

                    foreach (var property in mappedProperties)
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

                    var sourceInstance = Expression.New(sourceType);
                    var targetInstance = Expression.New(targetType);

                    var targetProperty = Expression.Property(argument, _propertyToInclude);
                    var propertiesToSelect = ResolveFieldContextHelpers.GetProperties(targetType, context.SubFields, model);
                    var targetBindings = propertiesToSelect.Select(propertyType =>
                        Expression.Bind(propertyType, Expression.Property(targetProperty, propertyType)));

                    var initializeTargetInstance = Expression.MemberInit(targetInstance, targetBindings);

                    var sourceBindings = mappedProperties.Select(x => x.Key.PropertyInfo).Select(propertyType =>
                        Expression.Bind(propertyType, Expression.Property(argument, propertyType))).ToList();
                    sourceBindings.Add(Expression.Bind(_propertyToInclude, initializeTargetInstance));

                    var initializeSourceInstance = Expression.MemberInit(sourceInstance, sourceBindings);
                    var selectLambda = Expression.Lambda<Func<TSourceType, TSourceType>>(initializeSourceInstance, argument);

                    query = query.Select(selectLambda);

                    var result = await query.ToListAsync();

                    var response = new Dictionary<Dictionary<IProperty, object>, TReturnType>();

                    foreach (var keyProperty in keyProperties)
                    {
                        var value = result;

                        foreach (var property in keyProperty)
                        {
                            value = value
                                .Where(x => sourceType
                                    .GetProperty(property.Key.Name)
                                    .GetValue(x)
                                    .Equals(property.Value))
                                .ToList();
                        }

                        if (value.Any())
                        {
                            response.Add(keyProperty, (TReturnType)value.Select(x => sourceType
                                .GetProperty(_propertyToInclude.Name)
                                .GetValue(x)).First());
                        }
                    }

                    return response;
                });

            return loader.LoadAsync(keyValues);
        });
    }
}