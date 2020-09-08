using System;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class FieldQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty>
        where TDbContext : DbContext
        where TProperty : class
    {
        private readonly FieldBuilder<TSourceType, TReturnType> _field;
        private readonly TDbContext _dbContext;
        private Func<IQueryable<TProperty>, IResolveFieldContext<object>, IQueryable<TProperty>> _businessLogic;
        private IQueryable<TProperty> _query { get; set; }

        public FieldQueryBuilder(FieldBuilder<TSourceType, TReturnType> field, TDbContext dbContext)
        {
            _field = field;
            _dbContext = dbContext;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty> Set(Expression<Func<TDbContext, DbSet<TProperty>>> accessor)
        {
            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            _query = (IQueryable<TProperty>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                .MakeGenericMethod(type)
                .Invoke(_dbContext, null);

            return this;
        }

        public FieldQueryBuilder<TSourceType, TReturnType, TDbContext, TProperty> Apply(
            Func<IQueryable<TProperty>, IResolveFieldContext<object>, IQueryable<TProperty>> businessLogic)
        {
            _businessLogic = businessLogic;

            return this;
        }

        public void ResolveListAsync()
        {
            _field.Filtered();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;

                ApplyWhereClauses(context);

                return (dynamic)await _query
                    .SelectFromContext(context, _dbContext.Model)
                    .ToListAsync();
            });
        }

        public void ResolvePropertyAsync()
        {
            _field.Filtered();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveFieldContext<object>)typedContext;

                ApplyWhereClauses(context);

                return (dynamic)await _query
                    .SelectFromContext(context, _dbContext.Model)
                    .FirstAsync();
            });
        }

        private void ApplyWhereClauses(IResolveFieldContext<object> context)
        {
            if (_businessLogic == default)
            {
                return;
            }

            _query = _businessLogic(_query, context);
        }
    }
}