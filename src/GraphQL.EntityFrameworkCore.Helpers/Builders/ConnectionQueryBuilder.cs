using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public class ConnectionQueryBuilder<TSourceType, TReturnType> : QueryBuilderBase<TReturnType, IResolveConnectionContext<object>>
        where TReturnType : class
    {
        private readonly ConnectionBuilder<TSourceType> _field;
        private readonly Type _targetType;
        private readonly Type _dbContextType;

        public ConnectionQueryBuilder(ConnectionBuilder<TSourceType> field, Type targetType, Type dbContextType = null)
        {
            _field = field;
            _targetType = targetType;
            _dbContextType = dbContextType != null ? dbContextType : DbContextTypeAccessor.DbContextType;
        }

        public ConnectionQueryBuilder<TSourceType, TReturnType> Apply(
            Func<IQueryable<TReturnType>, IResolveConnectionContext<object>, IQueryable<TReturnType>> businessLogic)
        {
            BusinessLogic = businessLogic;

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

                if (context.RequestServices == default)
                {
                    throw new Exception("ExecutionOptions.RequestServices is not defined (passed to ExecuteAsync), use GraphQL Server 4.0 and on");
                }

                var dbContext = (DbContext)context.RequestServices.GetRequiredService(_dbContextType);
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