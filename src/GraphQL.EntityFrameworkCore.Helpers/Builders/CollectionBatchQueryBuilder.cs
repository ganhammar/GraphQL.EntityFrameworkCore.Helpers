using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class CollectionBatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, IEnumerable<TReturnType>> _field;
        private readonly TDbContext _dbContext;
        private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;
        private readonly PropertyInfo _propertyToInclude;
        private readonly PropertyInfo _targetProperty;

        public CollectionBatchQueryBuilder(
            FieldBuilder<TSourceType, IEnumerable<TReturnType>> field,
            TDbContext dbContext,
            IDataLoaderContextAccessor dataLoaderContextAccessor,
            Expression<Func<TSourceType, IEnumerable<TProperty>>> collectionToInclude,
            Expression<Func<TReturnType, TKey>> keyProperty)
        {
            _field = field;
            _dbContext = dbContext;
            _dataLoaderContextAccessor = dataLoaderContextAccessor;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, IEnumerable<TProperty>>(collectionToInclude);
            _targetProperty = FieldHelpers.GetPropertyInfo<TReturnType, TKey>(keyProperty);
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var targetType = typeof(TReturnType);
            var loaderName = $"DataLoader_GET_{sourceType.Name}_{_propertyToInclude.Name}";
            var sourceProperty = GetSourceProperty(sourceType);

            if (sourceType != targetType)
            {
                ResolveOneToMany(sourceType, targetType, sourceProperty, loaderName);
            }
            else
            {
                ResolveManyToMany(sourceType, targetType, sourceProperty, loaderName);
            }
        }

        public void ResolveOneToMany(Type sourceType, Type targetType, IProperty sourceProperty, string loaderName)
        {
            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<TKey, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                            .MakeGenericMethod(targetType)
                            .Invoke(_dbContext, null);

                        query = Where(query, targetType, GetContainsLambda(sourceProperties, targetType));

                        query = query
                            .SelectFromContext((IResolveFieldContext<object>)context, _dbContext.Model);
                        
                        var result = await query.ToListAsync();

                        return result
                            .Select(x => new KeyValuePair<TKey, TReturnType>(
                                (TKey)targetType.GetProperty(_targetProperty.Name).GetValue(x),
                                x))
                            .ToLookup(x => x.Key, x => x.Value);
                    });

                return loader.LoadAsync((TKey)sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            });
        }

        public void ResolveManyToMany(Type sourceType, Type targetType, IProperty sourceProperty, string loaderName)
        {
            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<TKey, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                            .MakeGenericMethod(targetType)
                            .Invoke(_dbContext, null);
                        
                        query = Include(query, targetType);

                        var argument = Expression.Parameter(targetType);
                        var property = Expression.MakeMemberAccess(argument, _propertyToInclude);

                        MethodInfo anyMethod = QueryableExtensions.GetAnyMethod()
                            .MakeGenericMethod(targetType);
                        
                        var check = Expression.Call(anyMethod, property, GetContainsLambda(sourceProperties, targetType));
                        
                        query = Where(query, targetType, Expression.Lambda(check, argument));

                        query = query
                            .Filter((IResolveFieldContext<object>)context, _dbContext.Model);

                        var result = await query.ToListAsync();

                        return result.SelectMany(x => ((IEnumerable<TReturnType>)targetType
                            .GetProperty(_propertyToInclude.Name)
                            .GetValue(x))
                            .Select(y => new KeyValuePair<TKey, TReturnType>(
                                (TKey)targetType.GetProperty(_targetProperty.Name).GetValue(y),
                                x)))
                            .ToLookup(x => x.Key, x => x.Value);
                    });

                return loader.LoadAsync((TKey)sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            });
        }

        private LambdaExpression GetContainsLambda(IEnumerable<TKey> sourceProperties, Type targetType)
        {
            var argument = Expression.Parameter(targetType);
            var property = Expression.MakeMemberAccess(argument, _targetProperty);
            var collectionType = typeof(ICollection<>).MakeGenericType(typeof(TKey));
            var method = collectionType.GetMethod("Contains");
            var properties = Expression.Constant(sourceProperties, collectionType);
            var check = Expression.Call(properties, method, property);
            return Expression.Lambda(check, argument);
        }

        private IQueryable<TReturnType> Include(IQueryable<TReturnType> query, Type targetType)
        {
            var argument = Expression.Parameter(targetType);
            var property = Expression.MakeMemberAccess(argument, _propertyToInclude);
            var lambda = Expression.Lambda(property, argument);
            var method = QueryableExtensions.GetIncludeMethod();
            MethodInfo genericMethod = method
                .MakeGenericMethod(targetType, typeof(IEnumerable<TReturnType>));

            return (IQueryable<TReturnType>)genericMethod
                .Invoke(genericMethod, new object[] { query, lambda });
        }

        private IQueryable<TReturnType> Where(IQueryable<TReturnType> query, Type targetType, LambdaExpression lambda)
        {
            var whereMethod = QueryableExtensions.GetWhereMethod();
            MethodInfo genericMethod = whereMethod
                .MakeGenericMethod(targetType);

            return (IQueryable<TReturnType>)genericMethod
                .Invoke(genericMethod, new object[] { query, lambda });
        }

        private IProperty GetSourceProperty(Type sourceType)
        {
            var model = _dbContext.Model;
            var entity = model.FindEntityType(sourceType);
            var navigationProperties = entity.GetNavigations();

            var property = navigationProperties
                .Where(x => x.Name == _propertyToInclude.Name)
                .First();
            
            if (property.DeclaringType.Name != sourceType.FullName)
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