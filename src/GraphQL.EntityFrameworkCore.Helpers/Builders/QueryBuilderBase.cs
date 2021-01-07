using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GraphQL.EntityFrameworkCore.Helpers
{
    public partial class QueryBuilderBase<TReturnType, TContextType>
        where TContextType : IResolveFieldContext
    {
        protected Func<IQueryable<TReturnType>, TContextType, IQueryable<TReturnType>> BusinessLogic { get; set; }
        protected Func<TContextType, ValidationResult> ValidationAction { get; set; }
        protected Func<TContextType, Task<ValidationResult>> AsyncValidationAction { get; set; }
        protected Func<IResolveFieldContext<object>, Expression<Func<TReturnType, bool>>> BusinuessCheck { get; set; }

        protected async Task<bool> ValidateBusiness(TContextType context, IModel model)
        {
            if (ValidationAction != default)
            {
                return AddErrors(context, ValidationAction(context));
            }
            else if (AsyncValidationAction != default)
            {
                return AddErrors(context, await AsyncValidationAction(context));
            }

            return true;
        }

        protected bool ValidateConnectionInput(TContextType context, IConnectionInput<TReturnType> input, IModel model)
        {
            return AddErrors(context, input.Validate<TReturnType, TReturnType>(model));
        }

        protected bool ValidateFilterInput(IResolveFieldContext<object> context)
        {
            return AddErrors((TContextType)context, context.ValidateFilterInput());
        }

        private static bool AddErrors(TContextType context, ValidationResult result)
        {
            if (result.IsValid == false || result.Failures.Any() == true)
            {
                if (result.Failures.Any())
                {
                    result.Failures.ToList().ForEach(x => 
                    {
                        var data = new Dictionary<string, string>();

                        if (x.FieldName != default)
                        {
                            data.Add("FieldName", x.FieldName);
                        }

                        context.Errors.Add(new ExecutionError(x.Message, data));
                    });
                }
                else
                {
                    context.Errors.Add(new ExecutionError("Query is not valid"));
                }
            }

            return result.IsValid;
        }

        protected IQueryable<TReturnType> ApplyBusinessLogic(IQueryable<TReturnType> query, TContextType context)
        {
            if (BusinessLogic == default)
            {
                return query;
            }

            return BusinessLogic(query, context);
        }
    }
}