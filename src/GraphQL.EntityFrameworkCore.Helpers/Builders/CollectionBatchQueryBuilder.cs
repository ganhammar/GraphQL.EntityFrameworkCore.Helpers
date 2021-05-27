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

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> : QueryBuilderBase<TReturnType, IResolveFieldContext<object>>
    {
        private readonly FieldBuilder<TSourceType, IEnumerable<TReturnType>> _field;

        public CollectionBatchQueryBuilder(
            FieldBuilder<TSourceType, IEnumerable<TReturnType>> field,
            Expression<Func<TSourceType, IEnumerable<TProperty>>> collectionToInclude,
            Type dbContextType = null)
        {
            _field = field;
            _propertyToInclude = FieldHelpers
                .GetPropertyInfo<TSourceType, IEnumerable<TProperty>>(collectionToInclude);
            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> Where(
            Func<IResolveFieldContext<object>, Expression<Func<TReturnType, bool>>> clause)
        {
            BusinuessCheck = clause;

            return this;
        }

        public CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        public void ResolveAsync() => _field.ResolveAsync(typedContext =>
        {
            var context = (IResolveFieldContext<object>)typedContext;

            var dbContext = (DbContext)context.GetService(_dbContextType);
            var model = dbContext.Model;

            var sourceType = typeof(TSourceType);
            var targetType = typeof(TReturnType);
            var keyValues = GetKeyValuePairs(sourceType, model, context);

            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            var dataLoaderContextAccessor = context.GetService<IDataLoaderContextAccessor>();
            var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<Dictionary<IProperty, object>, TReturnType>(
                loaderName,
                async (keyProperties) =>
                {
                    if (await IsValid(context, dbContext.Model) == false)
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
                    var targetArgument = Expression.Parameter(targetType);
                    Expression targetMemberAccess = Expression.MakeMemberAccess(argument, _propertyToInclude);
                    targetMemberAccess = ApplyFilters(
                        targetMemberAccess, context, dbContext, targetType, argument, targetArgument, whereMethod);

                    var mappedProperties = MapProperties(keyProperties);
                    query = FilterBasedOnKeyProperties(query, mappedProperties, argument, whereMethod);

                    var sourceInstance = Expression.New(sourceType);

                    var initializeTargetInstance = InitTarget(
                        targetType, argument, targetArgument, targetMemberAccess, context, model);
                    var sourceBindings = mappedProperties
                        .Select(x => x.Key.PropertyInfo)
                        .Select(propertyType =>
                            Expression.Bind(propertyType, Expression.Property(argument, propertyType)))
                        .ToList();
                    sourceBindings.Add(Expression.Bind(_propertyToInclude, initializeTargetInstance));

                    var initializeSourceInstance = Expression.MemberInit(sourceInstance, sourceBindings);
                    var selectLambda = Expression.Lambda<Func<TSourceType, TSourceType>>(initializeSourceInstance, argument);

                    query = query.Select(selectLambda);

                    var result = await query.ToListAsync();

                    return MapResponse(keyProperties, result, sourceType);
                });
            
            return loader.LoadAsync(keyValues);
        });

        private Expression ApplyFilters(
            Expression memberAccess,
            IResolveFieldContext<object> context,
            DbContext dbContext,
            Type targetType,
            ParameterExpression argument,
            ParameterExpression targetArgument,
            MethodInfo whereMethod)
        {
            var model = dbContext.Model;
            var filters = FilterList.GetFilter(context);
            var expressions = filters != default
                ? FilterList.GetExpressions(targetArgument, targetType, filters, context, model)
                : new List<Expression>();
            var anyMethod = QueryableExtensions.GetAnyMethod()
                .MakeGenericMethod(targetType); 

            if (expressions.Any() || BusinuessCheck != default)
            {
                foreach (var expression in expressions)
                {
                    var lambda = Expression.Lambda<Func<TReturnType, bool>>(
                        expression, new ParameterExpression[] { targetArgument });
                    memberAccess = Expression.Call(typeof(Enumerable), nameof(Enumerable.Where),
                        new [] { targetType }, memberAccess, lambda);
                }

                if (BusinuessCheck != default)
                {
                    var expression = BusinuessCheck(context);

                    if (expression != default)
                    {
                        memberAccess = Expression.Call(typeof(Enumerable), nameof(Enumerable.Where),
                            new [] { targetType }, memberAccess, expression);
                    }
                }
                
                return memberAccess;
            }

            return memberAccess;
        }

        private Expression InitTarget(
            Type targetType,
            ParameterExpression argument,
            ParameterExpression targetArgument,
            Expression targetMemberAccess,
            IResolveFieldContext<object> context,
            IModel model)
        {
            var targetInstance = Expression.New(targetType);

            var propertiesToSelect = ResolveFieldContextHelpers
                .GetProperties(targetType, context.SubFields, model, context);
            var targetBindings = propertiesToSelect.Select(propertyType =>
                Expression.Bind(propertyType, Expression.Property(targetArgument, propertyType)));
            var memberInit = Expression.MemberInit(targetInstance, targetBindings);
            var lambda = Expression.Lambda(memberInit, targetArgument);
            var selectMethod = QueryableExtensions
                .GetSelectMethod()
                .MakeGenericMethod(targetType, targetType);
            return Expression.Call(selectMethod, targetMemberAccess, lambda);
        }

        private ILookup<Dictionary<IProperty, object>, TReturnType> MapResponse(
            IEnumerable<Dictionary<IProperty, object>> keyProperties,
            IEnumerable<TSourceType> result,
            Type sourceType)
        {
            var response = new List<KeyValuePair<Dictionary<IProperty, object>, TReturnType>>();

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
                    var list = value.SelectMany(x => (IEnumerable<TReturnType>)sourceType
                        .GetProperty(_propertyToInclude.Name)
                        .GetValue(x));
                    
                    foreach (var item in list)
                    {
                        response.Add(new KeyValuePair<Dictionary<IProperty, object>, TReturnType>(
                            keyProperty, item));
                    }
                }
            }

            return response.ToLookup(x => x.Key, x => x.Value);
        }
    }
}