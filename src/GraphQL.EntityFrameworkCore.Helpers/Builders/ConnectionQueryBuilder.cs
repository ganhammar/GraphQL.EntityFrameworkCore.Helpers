using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> : QueryBuilderBase<TReturnType, IResolveConnectionContext<object>>
        where TDbContext : DbContext
        where TReturnType : class
    {
        private readonly ConnectionBuilder<TSourceType> _field;
        private readonly TDbContext _dbContext;
        private IQueryable<TReturnType> _query { get; set; }

        public ConnectionQueryBuilder(ConnectionBuilder<TSourceType> field, TDbContext dbContext)
        {
            _field = field;
            _dbContext = dbContext;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> Set(
            Expression<Func<TDbContext, DbSet<TReturnType>>> accessor)
        {
            var type = FieldHelpers.GetPropertyInfo(accessor).PropertyType
                .GetGenericArguments().First();

            _query = (IQueryable<TReturnType>)typeof(DbContext).GetMethod(nameof(DbContext.Set))
                .MakeGenericMethod(type)
                .Invoke(_dbContext, null);

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> Apply(
            Func<IQueryable<TReturnType>, IResolveConnectionContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            BusinessLogic = businessLogic;

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType, TDbContext> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        public void ResolveAsync(Type connectionInputType)
        {
            _field.Paged();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveConnectionContext<object>)typedContext;
                var isValid = await ValidateBusiness(context, _dbContext.Model);

                if (!isValid && ValidateFilterInput(context))
                {
                    return default;
                }

                _query = ApplyBusinessLogic(_query, context);

                var input = (IConnectionInput<TReturnType>)Activator.CreateInstance(connectionInputType);
                input.SetConnectionInput(context);

                if (!ValidateConnectionInput(context, input, _dbContext.Model))
                {
                    return default;
                }

                return await _query
                    .ToConnection(input, _dbContext.Model);
            });
        }
    }
}