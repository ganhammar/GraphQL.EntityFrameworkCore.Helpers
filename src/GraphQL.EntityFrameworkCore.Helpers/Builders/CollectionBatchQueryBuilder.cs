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
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class CollectionBatchQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty, TKey>
        where TDbContext : DbContext
    {
        private readonly FieldBuilder<TSourceType, IEnumerable<TReturnType>> _field;
        private readonly HelperFieldBuilder<TSourceType, IEnumerable<TReturnType>, TProperty> _helperField;
        private readonly TDbContext _dbContext;
        private readonly IDataLoaderContextAccessor _dataLoaderContextAccessor;
        private readonly PropertyInfo _propertyToInclude;
        private readonly PropertyInfo _targetProperty;
        private readonly bool _isManyToMany;

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

        public CollectionBatchQueryBuilder(
            HelperFieldBuilder<TSourceType, IEnumerable<TReturnType>, TProperty> helperField,
            TDbContext dbContext,
            IDataLoaderContextAccessor dataLoaderContextAccessor,
            List<string> propertyPath,
            Expression<Func<TReturnType, TKey>> keyProperty)
        {
            var sourceType = typeof(TSourceType);
            var returnType = typeof(TReturnType);

            _helperField = helperField;
            _dbContext = dbContext;
            _dataLoaderContextAccessor = dataLoaderContextAccessor;
            _propertyToInclude = returnType.GetProperty(propertyPath.First());
            _targetProperty = GetTargetProperty(sourceType, _propertyToInclude).PropertyInfo;
            _isManyToMany = true;
        }

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var targetType = typeof(TReturnType);
            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            var sourceProperty = GetSourceProperty(sourceType);

            if (_isManyToMany == false && sourceType != targetType)
            {
                ResolveOneToMany(sourceType, targetType, sourceProperty, loaderName);
            }
            else
            {
                ResolveManyToMany(sourceType, targetType, sourceProperty, loaderName);
            }
        }

        private void ResolveOneToMany(Type sourceType, Type targetType, IProperty sourceProperty, string loaderName)
        {
            _field.ResolveAsync(context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<object, TReturnType>(
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
                            .Select(x => new KeyValuePair<object, TReturnType>(
                                targetType.GetProperty(_targetProperty.Name).GetValue(x),
                                x))
                            .ToLookup(x => x.Key, x => x.Value);
                    });

                return loader.LoadAsync(sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            });
        }

        private void ResolveManyToMany(Type sourceType, Type returnType, IProperty sourceProperty, string loaderName)
        {
            Func<IResolveFieldContext<TSourceType>, Task<IEnumerable<TReturnType>>> action = context =>
            {
                var loader = _dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<object, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                            .MakeGenericMethod(returnType)
                            .Invoke(_dbContext, null);
                        
                        query = Include(query, returnType);

                        var argument = Expression.Parameter(returnType);
                        var property = Expression.MakeMemberAccess(argument, _propertyToInclude);

                        var targetType = returnType == sourceType ? returnType : _targetProperty.DeclaringType;

                        MethodInfo anyMethod = QueryableExtensions.GetAnyMethod()
                            .MakeGenericMethod(targetType);
                        
                        var check = Expression.Call(anyMethod, property, GetContainsLambda(sourceProperties, targetType));
                        
                        query = Where(query, returnType, Expression.Lambda(check, argument));

                        query = query
                            .Filter((IResolveFieldContext<object>)context, _dbContext.Model);

                        var result = await query.ToListAsync();

                        return SelectManyToMany(result, returnType);
                    });

                return loader.LoadAsync(sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            };

            if (_field != default)
            {
                _field.ResolveAsync(action);
            }
            else if (_helperField != default)
            {
                _helperField.ResolveAsync(action);
            }
        }
        
        private ILookup<object, TReturnType> SelectManyToMany(List<TReturnType> result, Type returnType)
        {
            var mainArgument = Expression.Parameter(returnType);
            var property = Expression.MakeMemberAccess(mainArgument, _propertyToInclude);

            var innerArgument = Expression.Parameter(_targetProperty.DeclaringType);
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(typeof(object), returnType);
            var constructor = keyValuePairType.GetConstructor(new Type[] { typeof(object), returnType });
            var lambda = Expression.Lambda(Expression.New(constructor, new Expression[]
            {
                Expression.Convert(Expression.Property(innerArgument, _targetProperty), typeof(object)),
                mainArgument,
            }), innerArgument);

            var selectMethod = QueryableExtensions.GetSelectMethod()
                .MakeGenericMethod(
                    _targetProperty.DeclaringType,
                    keyValuePairType
                );
            var select = Expression.Call(selectMethod, property, lambda);

            var selectManyMethod = QueryableExtensions.GetSelectManyMethod()
                .MakeGenericMethod(
                    returnType,
                    keyValuePairType
                );
            var selectMany = (IEnumerable<KeyValuePair<object, TReturnType>>)selectManyMethod
                .Invoke(selectManyMethod, new object[]
                {
                    result,
                    Expression.Lambda(select, mainArgument).Compile()
                });
            
            return selectMany.ToLookup(x => x.Key, x => x.Value);
        }

        private LambdaExpression GetContainsLambda(IEnumerable<object> sourceProperties, Type targetType)
        {
            var castMethod = QueryableExtensions.GetCastMethod().MakeGenericMethod(_targetProperty.PropertyType);
            var castedSourceProperties = castMethod.Invoke(castMethod, new object[] { sourceProperties });
            var argument = Expression.Parameter(targetType);
            var property = Expression.MakeMemberAccess(argument, _targetProperty);
            var properties = Expression.Constant(castedSourceProperties);
            var check = Expression.Call(typeof(Enumerable), "Contains", new[] { _targetProperty.PropertyType }, properties, property);
            return Expression.Lambda(check, argument);
        }

        private IQueryable<TReturnType> Include(IQueryable<TReturnType> query, Type targetType)
        {
            var argument = Expression.Parameter(targetType);
            var property = Expression.MakeMemberAccess(argument, _propertyToInclude);
            var lambda = Expression.Lambda(property, argument);

            var method = QueryableExtensions.GetIncludeMethod();
            var enumerableType = typeof(IEnumerable<>)
                .MakeGenericType(_propertyToInclude.PropertyType.GenericTypeArguments[0]);
            MethodInfo genericMethod = method
                .MakeGenericMethod(targetType, enumerableType);

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

        private IProperty GetTargetProperty(Type sourceType, PropertyInfo propertyToInclude)
        {
            var model = _dbContext.Model;
            var entity = model.FindEntityType(sourceType);
            var navigationProperties = entity.GetNavigations();

            var property = navigationProperties
                .Where(x => x.Name == propertyToInclude.Name)
                .First();
            
            if (property.DeclaringType.Name == sourceType.FullName)
            {
                var foreignKey = property.ForeignKey.Properties.First();

                if (foreignKey.PropertyInfo == default)
                {
                    throw new Exception($@"All key fields must be mapped in data entity, missing key
                        field for {propertyToInclude.Name} in {foreignKey.DeclaringType.Name}");
                }

                return foreignKey;
            }

            return property.ForeignKey
                .PrincipalKey
                .Properties
                .Where(x => x.PropertyInfo != null)
                .First();
        }
    }
}