using GraphQL.DataLoader;
using GraphQL.EntityFrameworkCore.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLEntityFrameworkCoreHelpers<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            DbContextTypeAccessor.DbContextType = typeof(TDbContext);

            return services.AddGraphQLEntityFrameworkCoreHelpers();
        }

        public static IServiceCollection AddGraphQLEntityFrameworkCoreHelpers(this IServiceCollection services)
        {
            services.AddSingleton<FilterableModesGraphType>();
            services.AddSingleton<FilterableFieldOperatorsGraphType>();
            services.AddSingleton<FilterableValueOperatorsGraphType>();
            services.AddSingleton<FilterableInputFieldGraphType>();
            services.AddSingleton<FilterableInputGraphType>();
            services.AddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            
            return services;
        }
    }
}
