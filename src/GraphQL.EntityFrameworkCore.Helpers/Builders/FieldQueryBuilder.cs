using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FieldQueryBuilder<TSourceType, TReturnType, TProperty> : QueryBuilderBase<TProperty, IResolveFieldContext<object>>
        where TProperty : class
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly Type _targetType;
        private Func<IResolveFieldContext<object>, IQueryable<TProperty>, IQueryable<TProperty>> _customQueryBuilderLogic = null;

        public FieldQueryBuilder(FieldBuilder<TSourceType, TReturnType> field, Type targetType, Type dbContextType = null)
        {
            _field = field;
            _targetType = targetType;
            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TProperty> Where(
            Func<IResolveFieldContext<object>, Expression<Func<TProperty, bool>>> clause)
        {
            BusinuessCheck = clause;

            return this;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TProperty> Apply(
            Func<IResolveFieldContext<object>, IQueryable<TProperty>, IQueryable<TProperty>> customQueryBuilderLogic)
        {
            _customQueryBuilderLogic = customQueryBuilderLogic;
            return this;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TProperty> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TProperty> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        private async Task<IQueryable<TProperty>> GetQuery(IResolveFieldContext<object> context)
        {
            var dbContext = (DbContext)context.GetService(_dbContextType);

            if (await IsValid(context, dbContext.Model) == false)
            {
                return default;
            }

            var query = (IQueryable<TProperty>)QueryableExtensions.GetSetMethod<TProperty>()
                .MakeGenericMethod(_targetType)
                .Invoke(dbContext, null);

            query = _customQueryBuilderLogic != null
                ? _customQueryBuilderLogic(context, query) : query;
            query = ApplyBusinessCheck(query, context);

            return query.SelectFromContext(context, dbContext.Model);
        }

        public void ResolveCollectionAsync()
        {
            _field.Filtered();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;
                var query = await GetQuery(context);

                if (query == default)
                {
                    return default;
                }

                return (dynamic)await query
                    .ToListAsync();
            });
        }

        public void ResolvePropertyAsync()
        {
            _field.Filtered();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;
                var query = await GetQuery(context);

                if (query == default)
                {
                    return default;
                }

                return (dynamic)await query
                    .FirstAsync();
            });
        }
    }
}