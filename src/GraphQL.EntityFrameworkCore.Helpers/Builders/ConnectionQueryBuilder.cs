using System;
using System.Linq;
using System.Linq.Expressions;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext>
        where TDbContext : DbContext
        where TReturnType : class
    {
        private readonly ConnectionBuilder<TSourceType> _field;
        private readonly TDbContext _dbContext;
        private Func<IQueryable<TReturnType>, IResolveConnectionContext<object>, IQueryable<TReturnType>> _businessLogic;
        private IQueryable<TReturnType> _query { get; set; }

        public ConnectionQueryBuilder(ConnectionBuilder<TSourceType> field, TDbContext dbContext)
        {
            _field = field;
            _dbContext = dbContext;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> Set(Expression<Func<TDbContext, DbSet<TReturnType>>> accessor)
        {
            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            _query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                .MakeGenericMethod(type)
                .Invoke(_dbContext, null);

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> Where(Func<IQueryable<TReturnType>, IResolveConnectionContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            _businessLogic = businessLogic;

            return this;
        }

        public void ResolveAsync(Type connectionInputType)
        {
            _field.Paged();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveConnectionContext<object>)typedContext;

                ApplyWhereClauses(context);

                var input = (IConnectionInput<TReturnType>)Activator.CreateInstance(connectionInputType);
                input.SetConnectionInput(context);

                return await _query
                    .ToConnection(input, _dbContext.Model);
            });
        }

        private void ApplyWhereClauses(IResolveConnectionContext<object> context)
        {
            if (_businessLogic == default)
            {
                return;
            }

            _query = _businessLogic(_query, context);
        }
    }
}