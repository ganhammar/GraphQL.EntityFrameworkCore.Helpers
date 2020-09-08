using System;
using System.Linq;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public partial class QueryBuilderBase<TReturnType, TContextType>
    {
        private Func<IQueryable<TReturnType>, TContextType, IQueryable<TReturnType>> _businessLogic { get; set; }

        protected void SetBusinessLogic(
            Func<IQueryable<TReturnType>, TContextType, IQueryable<TReturnType>> businessLogic)
        {
            _businessLogic = businessLogic;
        }

        protected IQueryable<TReturnType> ApplyBusinessLogic(IQueryable<TReturnType> query, TContextType context)
        {
            if (_businessLogic == default)
            {
                return query;
            }

            return _businessLogic(query, context);
        }
    }
}