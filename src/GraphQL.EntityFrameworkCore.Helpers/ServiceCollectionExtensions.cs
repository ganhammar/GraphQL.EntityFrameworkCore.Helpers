using GraphQL.EntityFrameworkCore.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLEntityFrameworkHelpers(this IServiceCollection services, DbContext dbContext)
        {
            SelectFromFields.Model = dbContext.Model;
            
            return services;
        }
    }
}
