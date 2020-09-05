using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class BatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly TDbContext _dbContext;
        private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;
        private readonly PropertyInfo _propertyToInclude;
        private readonly PropertyInfo _targetProperty;

        public BatchQueryBuilder(
            FieldBuilder<TSourceType, TReturnType> field,
            TDbContext dbContext,
            IDataLoaderContextAccessor dataLoaderContextAccessor,
            Expression<Func<TSourceType, TProperty>> propertyToInclude,
            Expression<Func<TReturnType, TKey>> targetProperty)
        {
            _field = field;
            _dbContext = dbContext;
            _dataLoaderContextAccessor = dataLoaderContextAccessor;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, TProperty>(propertyToInclude);
            _targetProperty = FieldHelpers.GetPropertyInfo<TReturnType, TKey>(targetProperty);
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var returnType = typeof(TReturnType);
            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            var sourceProperty = GetSourceProperty(sourceType);

            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddBatchLoader<TKey, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                            .MakeGenericMethod(returnType)
                            .Invoke(_dbContext, null);
                        
                        var argument = Expression.Parameter(returnType);
                        var property = Expression.MakeMemberAccess(argument, _targetProperty);
                        var properties = Expression.Constant(sourceProperties);
                        var check = Expression.Call(typeof(Enumerable), "Contains", new[] { typeof(TKey) }, properties, property);
                        var lambda = Expression.Lambda(check, argument);

                        var whereMethod = QueryableExtensions.GetWhereMethod();
                        MethodInfo genericMethod = whereMethod
                            .MakeGenericMethod(returnType);

                        query = (IQueryable<TReturnType>)genericMethod
                            .Invoke(genericMethod, new object[] { query, lambda });
                        
                        query = query
                            .SelectFromContext((IResolveFieldContext<object>)context, _dbContext.Model);

                        return await query
                            .ToDictionaryAsync(
                                x => (TKey)returnType.GetProperty(_targetProperty.Name).GetValue(x),
                                x => x);
                    });

                return loader.LoadAsync((TKey)sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            });
        }

        private IProperty GetSourceProperty(Type sourceType)
        {
            var model = _dbContext.Model;
            var entity = model.FindEntityType(sourceType);
            var navigationProperties = entity.GetNavigations();

            var property = navigationProperties
                .Where(x => x.Name == _propertyToInclude.Name)
                .First();
            
            if (property.DeclaringType.Name == sourceType.FullName)
            {
                var foreignKey = property.ForeignKey.Properties.First();

                if (foreignKey.PropertyInfo != default)
                {
                    return foreignKey;
                }
            }

            return property.ForeignKey
                .PrincipalKey
                .Properties
                .Where(x => x.PropertyInfo != null)
                .First();
        }
    }
}