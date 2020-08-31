using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GraphQL.Builders;
using GraphQL.DataLoader;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class BatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly TDbContext _dbContext;
        private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;
        private readonly PropertyInfo _propertyToInclude;

        public BatchQueryBuilder(
            FieldBuilder<TSourceType, TReturnType> field,
            TDbContext dbContext,
            IDataLoaderContextAccessor dataLoaderContextAccessor,
            Expression<Func<TSourceType, TProperty>> propertyToInclude,
            Expression<Func<TSourceType, TKey>> keyProperty)
        {
            _field = field;
            _dbContext = dbContext;
            _dataLoaderContextAccessor = dataLoaderContextAccessor;
            _propertyToInclude = FieldHelpers.GetPropertyInfo<TSourceType, TProperty>(propertyToInclude);
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var loaderName = $"DataLoader_GET_{sourceType.Name}_{_propertyToInclude.Name}";
            var relationship = GetRelationship(sourceType);

            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddBatchLoader<TKey, TReturnType>(
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

                        return await query
                            .ToDictionaryAsync(
                                x => (TKey)targetType.GetProperty(relationship.Target.Name).GetValue(x),
                                x => x);
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

            return navigationProperties
                .Where(x => x.Name == _propertyToInclude.Name)
                .Select(x => new Relationship
                {
                    Source = x.ForeignKey.Properties
                        .Where(y => y.PropertyInfo != null)
                        .Select(y => y.PropertyInfo)
                        .First(),
                    Target = x.ForeignKey.PrincipalKey.Properties
                        .Where(y => y.PropertyInfo != null)
                        .Select(y => y.PropertyInfo)
                        .First(),
                })
                .First();
        }

        private class Relationship
        {
            public PropertyInfo Source { get; set; }
            public PropertyInfo Target { get; set; }
        }
    }
}