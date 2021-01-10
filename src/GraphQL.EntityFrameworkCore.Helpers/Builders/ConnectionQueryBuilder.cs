using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ConnectionQueryBuilder<TSourceType, TReturnType> : QueryBuilderBase<TReturnType, IResolveConnectionContext<object>>
        where TReturnType : class
    {
        private readonly ConnectionBuilder<TSourceType> _field;
        private readonly Type _targetType;

        public ConnectionQueryBuilder(ConnectionBuilder<TSourceType> field, Type targetType, Type dbContextType = null)
        {
            _field = field;
            _targetType = targetType;
            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType> Where(
            Func<IResolveFieldContext<object>, Expression<Func<TReturnType, bool>>> clause)
        {
            BusinuessCheck = clause;

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType> Validate(
            Func<IResolveFieldContext<object>, ValidationResult> action)
        {
            ValidationAction = action;

            return this;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType> ValidateAsync(
            Func<IResolveFieldContext<object>, Task<ValidationResult>> action)
        {
            AsyncValidationAction = action;

            return this;
        }

        public void ResolveAsync(Type connectionInputType = default)
        {
            if (connectionInputType == default)
            {
                connectionInputType = typeof(ConnectionInput<>).MakeGenericType(typeof(TReturnType));
            }

            _field.Paged();

            _field.ResolveAsync(async typedContext =>
            {
                var context = (IResolveConnectionContext<object>)typedContext;
                var dbContext = (DbContext)context.GetService(_dbContextType);
                var isValid = await ValidateBusiness(context, dbContext.Model);

                if (!isValid && ValidateFilterInput(context))
                {
                    return default;
                }

                var query = (IQueryable<TReturnType>)QueryableExtensions.GetSetMethod<TReturnType>()
                    .MakeGenericMethod(_targetType)
                    .Invoke(dbContext, null);

                query = ApplyBusinessLogic(query, context);

                var input = (IConnectionInput<TReturnType>)Activator.CreateInstance(connectionInputType);
                input.SetConnectionInput(context);

                if (!ValidateConnectionInput(context, input, dbContext.Model))
                {
                    return default;
                }

                return await query
                    .ToConnection(input, dbContext.Model);
            });
        }
    }
}