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
    public class CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> : QueryBuilderBase<TReturnType, IResolveFieldContext<object>>
    {
        private readonly FieldBuilder<TSourceType, IEnumerable<TReturnType>> _field;
        private readonly HelperFieldBuilder<TSourceType, IEnumerable<TReturnType>, TProperty> _helperField;
        private readonly PropertyInfo _propertyToInclude;
        private readonly bool _isManyToMany;
        private readonly Type _dbContextType;

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

        public CollectionBatchQueryBuilder(
            HelperFieldBuilder<TSourceType, IEnumerable<TReturnType>, TProperty> helperField,
            List<string> propertyPath,
            Type dbContextType = null)
        {
            var sourceType = typeof(TSourceType);
            var returnType = typeof(TReturnType);

            _helperField = helperField;
            _propertyToInclude = (propertyPath.Count == 1 ? sourceType : returnType)
                .GetProperty(propertyPath.First());

            if (propertyPath.Count > 2)
            {
                throw new Exception(@"Can't resolve relationships further than 
                    two steps apart (.MapsTo().ThenTo())");
            }
            else if (propertyPath.Count == 2)
            {
                _isManyToMany = true;
            }

            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public CollectionBatchQueryBuilder<TSourceType, TReturnType, TProperty> Apply(
            Func<IQueryable<TReturnType>, IResolveFieldContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            BusinessLogic = businessLogic;

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

        public void ResolveAsync()
        {
            var sourceType = typeof(TSourceType);
            var targetType = typeof(TReturnType);
            var loaderName = $"DataLoader_Get_{sourceType.Name}_{_propertyToInclude.Name}";
            Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IEnumerable<TReturnType>>> action;

            if (_isManyToMany == false && sourceType != targetType)
            {
                action = ResolveOneToMany(sourceType, targetType, loaderName);
            }
            else
            {
                action = ResolveManyToMany(sourceType, targetType, loaderName);
            }

            if (_field != default)
            {
                _field.ResolveAsync(action);
            }
            else if (_helperField != default)
            {
                _helperField.ResolveAsync(action);
            }
        }

        private Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IEnumerable<TReturnType>>> ResolveOneToMany(
            Type sourceType,
            Type targetType,
            string loaderName)
        {
            return typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;
                var dbContext = (DbContext)context.GetService(_dbContextType);
                var (sourceProperty, targetProperty) = GetRelationship(sourceType, dbContext.Model);
                var dataLoaderContextAccessor = context.GetService<IDataLoaderContextAccessor>();
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<object, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var isValid = await ValidateBusiness(context, dbContext.Model);

                        if (!isValid && ValidateFilterInput(context))
                        {
                            return default;
                        }

                        var query = (IQueryable<TReturnType>)QueryableExtensions.GetSetMethod<TReturnType>()
                            .MakeGenericMethod(targetType)
                            .Invoke(dbContext, null);

                        query = ApplyBusinessLogic(query, context);

                        query = Where(query, targetType, GetContainsLambda(sourceProperties, targetType, targetProperty));

                        query = query
                            .SelectFromContext(context, dbContext.Model);
                        
                        var result = await query.ToListAsync();

                        return result
                            .Select(x => new KeyValuePair<object, TReturnType>(
                                targetType.GetProperty(targetProperty.Name).GetValue(x),
                                x))
                            .ToLookup(x => x.Key, x => x.Value);
                    });

                return loader.LoadAsync(sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            };
        }

        private Func<IResolveFieldContext<TSourceType>, IDataLoaderResult<IEnumerable<TReturnType>>> ResolveManyToMany(
            Type sourceType,
            Type returnType,
            string loaderName)
        {
            return typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;
                var dbContext = (DbContext)context.GetService(_dbContextType);
                var (sourceProperty, targetProperty) = GetRelationship(sourceType, dbContext.Model);
                var dataLoaderContextAccessor = context.GetService<IDataLoaderContextAccessor>();
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader<object, TReturnType>(
                    loaderName,
                    async (sourceProperties) =>
                    {
                        var query = (IQueryable<TReturnType>)QueryableExtensions.GetSetMethod<TReturnType>()
                            .MakeGenericMethod(returnType)
                            .Invoke(dbContext, null);
                        
                        query = Include(query, returnType);

                        query = ApplyBusinessLogic(query, context);

                        var argument = Expression.Parameter(returnType);
                        var property = Expression.MakeMemberAccess(argument, _propertyToInclude);

                        var targetType = returnType == sourceType
                            ? returnType : targetProperty.PropertyInfo.DeclaringType;

                        MethodInfo anyMethod = QueryableExtensions.GetAnyMethod()
                            .MakeGenericMethod(targetType);
                        
                        var check = Expression.Call(anyMethod, property,
                            GetContainsLambda(sourceProperties, targetType, targetProperty));
                        
                        query = Where(query, returnType, Expression.Lambda(check, argument));

                        query = query
                            .Filter(context, dbContext.Model);

                        var result = await query.ToListAsync();

                        return SelectManyToMany(result, returnType, targetProperty);
                    });

                return loader.LoadAsync(sourceType
                    .GetProperty(sourceProperty.Name)
                    .GetValue(context.Source));
            };
        }
        
        private ILookup<object, TReturnType> SelectManyToMany(
            List<TReturnType> result, Type returnType, IProperty targetProperty)
        {
            var mainArgument = Expression.Parameter(returnType);
            var property = Expression.MakeMemberAccess(mainArgument, _propertyToInclude);

            var innerArgument = Expression.Parameter(targetProperty.PropertyInfo.DeclaringType);
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(typeof(object), returnType);
            var constructor = keyValuePairType.GetConstructor(new Type[] { typeof(object), returnType });
            var lambda = Expression.Lambda(Expression.New(constructor, new Expression[]
            {
                Expression.Convert(
                    Expression.Property(innerArgument, targetProperty.PropertyInfo),
                    typeof(object)),
                mainArgument,
            }), innerArgument);

            var selectMethod = QueryableExtensions.GetSelectMethod()
                .MakeGenericMethod(
                    targetProperty.PropertyInfo.DeclaringType,
                    keyValuePairType);
            var select = Expression.Call(selectMethod, property, lambda);

            var selectManyMethod = QueryableExtensions.GetSelectManyMethod()
                .MakeGenericMethod(
                    returnType,
                    keyValuePairType);
            var selectMany = (IEnumerable<KeyValuePair<object, TReturnType>>)selectManyMethod
                .Invoke(selectManyMethod, new object[]
                {
                    result,
                    Expression.Lambda(select, mainArgument).Compile()
                });
            
            return selectMany.ToLookup(x => x.Key, x => x.Value);
        }

        private LambdaExpression GetContainsLambda(
            IEnumerable<object> sourceProperties, Type targetType, IProperty targetProperty)
        {
            var castMethod = QueryableExtensions.GetCastMethod()
                .MakeGenericMethod(targetProperty.PropertyInfo.PropertyType);
            var castedSourceProperties = castMethod.Invoke(castMethod, new object[] { sourceProperties });
            var properties = Expression.Constant(castedSourceProperties);

            var argument = Expression.Parameter(targetType);
            var property = Expression.MakeMemberAccess(argument, targetProperty.PropertyInfo);

            var check = Expression.Call(typeof(Enumerable), "Contains",
                new[] { targetProperty.PropertyInfo.PropertyType }, properties, property);

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

        private (IProperty source, IProperty target) GetRelationship(Type sourceType, IModel model)
        {
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

            if (foreignKey.PropertyInfo == default && sourceType != typeof(TReturnType))
            {
                throw new Exception($@"All key fields must be mapped in data entity, missing key 
                    field for {_propertyToInclude.Name} in {foreignKey.DeclaringType.Name}");
            }

            var principal = property.ForeignKey
                .PrincipalKey
                .Properties
                .First();
            
            if (foreignKey.PropertyInfo == default && sourceType == typeof(TReturnType))
            {
                return (principal, principal);
            }

            if (principal.PropertyInfo == default)
            {
                throw new Exception($@"All key fields must be mapped in data entity, missing key 
                    field for {_propertyToInclude.Name} in {principal.DeclaringType.Name}");
            }
            
            if (property.DeclaringType.Name != sourceType.FullName)
            {
                return (foreignKey, principal);
            }

            return (principal, foreignKey);
        }
    }
}