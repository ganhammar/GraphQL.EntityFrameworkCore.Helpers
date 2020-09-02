using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class CollectionBatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, IEnumerable<TReturnType>> _field;
        private readonly TDbContext _dbContext;
        private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;
        private readonly PropertyInfo _propertyToInclude;

        public CollectionBatchQueryBuilder(
            FieldBuilder<TSourceType, IEnumerable<TReturnType>> field,
            TDbContext dbContext,
            IDataLoaderContextAccessor dataLoaderContextAccessor,
            Expression<Func<TSourceType, TKey>> keyProperty,
            Expression<Func<TSourceType, IEnumerable<TProperty>>> collectionToInclude)
        {
            _field = field;
            _dbContext = dbContext;
            _dataLoaderContextAccessor = dataLoaderContextAccessor;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, IEnumerable<TProperty>>(collectionToInclude);
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var loaderName = $"DataLoader_GET_{sourceType.Name}_{_propertyToInclude.Name}";
            var relationship = GetRelationship(sourceType);

            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<TKey, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var targetType = typeof(TReturnType);
                        var query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                            .MakeGenericMethod(targetType)
                            .Invoke(_dbContext, null);
                        
                        var argument = Expression.Parameter(targetType);
                        var property = Expression.MakeMemberAccess(argument, relationship.Target);
                        var collectionType = typeof(ICollection<>).MakeGenericType(typeof(TKey));
                        var method = collectionType.GetMethod("Contains");
                        var properties = Expression.Constant(sourceProperties, collectionType);
                        var check = Expression.Call(properties, method, property);
                        var lambda = Expression.Lambda(check, argument);

                        var whereMethod = QueryableExtensions.GetWhereMethod();
                        MethodInfo genericMethod = whereMethod
                            .MakeGenericMethod(targetType);

                        query = (IQueryable<TReturnType>)genericMethod
                            .Invoke(genericMethod, new object[] { query, lambda });
                        
                        query = query
                            .SelectFromContext((IResolveFieldContext<object>)context, _dbContext.Model);
                        
                        var result = await query.ToListAsync();

                        return result
                            .Select(x => new KeyValuePair<TKey, TReturnType>(
                                (TKey)targetType.GetProperty(relationship.Target.Name).GetValue(x),
                                x))
                            .ToLookup(x => x.Key, x => x.Value);
                    });

                return loader.LoadAsync((TKey)sourceType
                    .GetProperty(relationship.Source.Name)
                    .GetValue(context.Source));
            });
        }

        private Relationship GetRelationship(Type sourceType)
        {
            var model = _dbContext.Model;
            var entity = model.FindEntityType(sourceType);
            var navigationProperties = entity.GetNavigations();

            var properties = navigationProperties
                .Where(x => x.Name == _propertyToInclude.Name)
                .Select(x => new
                {
                    ForeignKey = x.ForeignKey.Properties
                        .Where(y => y.PropertyInfo != null)
                        .Select(y => y.PropertyInfo)
                        .First(),
                    PrincipalKey = x.ForeignKey.PrincipalKey.Properties
                        .Where(y => y.PropertyInfo != null)
                        .Select(y => y.PropertyInfo)
                        .First(),
                })
                .First();
            
            return new Relationship
            {
                Source = properties.ForeignKey.DeclaringType == sourceType
                    ? properties.ForeignKey : properties.PrincipalKey,
                Target = properties.ForeignKey.DeclaringType != sourceType
                    ? properties.ForeignKey : properties.PrincipalKey,
            };
        }

        private class Relationship
        {
            public PropertyInfo Source { get; set; }
            public PropertyInfo Target { get; set; }
        }
    }
}