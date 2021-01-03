using System;
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
    public class BatchQueryBuilder<TSourceType, TReturnType, TDbContext> : QueryBuilderBase<TReturnType, IResolveFieldContext<object>>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly TDbContext _dbContext;
        private readonly PropertyInfo _propertyToInclude;

        public BatchQueryBuilder(
            FieldBuilder<TSourceType, TReturnType> field,
            TDbContext dbContext,
            Expression<Func<TSourceType, TReturnType>> propertyToInclude)
        {
            _field = field;
            _dbContext = dbContext;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, TReturnType>(propertyToInclude);
        }

        public BatchQueryBuilder<TSourceType, TReturnType, TDbContext> Apply(
            Func<IQueryable<TReturnType>, IResolveFieldContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            BusinessLogic = businessLogic;

            return this;
        }

        public BatchQueryBuilder<TSourceType, TReturnType, TDbContext> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public BatchQueryBuilder<TSourceType, TReturnType, TDbContext> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var returnType = typeof(TReturnType);
            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            var (sourceProperty, targetProperty) = GetRelationship(sourceType);

            _field.ResolveAsync(typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;

                if (context.RequestServices == default)
                {
                    throw new Exception("ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on");
                }

                var dataLoaderContextAccessor = context.RequestServices.GetRequiredService<IDataLoaderContextAccessor>();
                var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader<object, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var isValid = await ValidateBusiness(context, _dbContext.Model);

                        if (!isValid && ValidateFilterInput(context))
                        {
                            return default;
                        }

                        var query = (IQueryable<TReturnType>)QueryableExtensions.GetSetMethod<TReturnType>()
                            .MakeGenericMethod(returnType)
                            .Invoke(_dbContext, null);
                        
                        query = ApplyBusinessLogic(query, context);
                        
                        var castMethod = QueryableExtensions.GetCastMethod()
                            .MakeGenericMethod(targetProperty.PropertyInfo.PropertyType);
                        var castedSourceProperties = castMethod.Invoke(castMethod, new object[] { sourceProperties });
                        var properties = Expression.Constant(castedSourceProperties);

                        var argument = Expression.Parameter(returnType);
                        var property = Expression.MakeMemberAccess(argument, targetProperty.PropertyInfo);

                        var check = Expression.Call(typeof(Enumerable), "Contains",
                            new[] { targetProperty.PropertyInfo.PropertyType }, properties, property);
                        var lambda = Expression.Lambda(check, argument);

                        var whereMethod = QueryableExtensions.GetWhereMethod();
                        MethodInfo genericMethod = whereMethod
                            .MakeGenericMethod(returnType);

                        query = (IQueryable<TReturnType>)genericMethod
                            .Invoke(genericMethod, new object[] { query, lambda });
                        
                        query = query
                            .SelectFromContext(context, _dbContext.Model);

                        return await query
                            .ToDictionaryAsync(
                                x => returnType.GetProperty(targetProperty.Name).GetValue(x),
                                x => x);
                    });

                return loader.LoadAsync(sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            });
        }

        private (IProperty source, IProperty target) GetRelationship(Type sourceType)
        {
            var model = _dbContext.Model;
            var entity = model.FindEntityType(sourceType);
            var navigationProperties = entity.GetNavigations();

            if (navigationProperties.Any() == false ||
                navigationProperties.Any(x => x.Name == _propertyToInclude.Name) == false)
            {
                throw new Exception($@"All relationships needs to mapped to automatically be able 
                    to resolve data loaded fields, missing navigation property for {_propertyToInclude.Name}");
            }

            var property = navigationProperties
                .Where(x => x.Name == _propertyToInclude.Name)
                .First();

            if (property.ForeignKey.Properties.Count > 1)
            {
                throw new Exception("Composite keys is not supported");
            }

            var foreignKey = property.ForeignKey.Properties.First();

            if (foreignKey.PropertyInfo == default)
            {
                throw new Exception($@"All key fields must be mapped in data entity, missing key
                    field for {_propertyToInclude.Name} in {foreignKey.DeclaringType.Name}");
            }

            var principal = property.ForeignKey
                .PrincipalKey
                .Properties
                .First();

            if (principal.PropertyInfo == default)
            {
                throw new Exception($@"All key fields must be mapped in data entity, missing key
                    field for {_propertyToInclude.Name} in {principal.DeclaringType.Name}");
            }

            if (property.DeclaringType.Name == sourceType.FullName)
            {
                return (foreignKey, principal);
            }

            return (principal, foreignKey);
        }
    }
}