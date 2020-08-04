using GraphQL.EntityFrameworkCore.Helpers.Filterable;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLEntityFrameworkCoreHelpers(this IServiceCollection services)
        {
            services.AddSingleton<FilterableModesGraphType>();
            services.AddSingleton<FilterableOperatorsGraphType>();
            services.AddSingleton<FilterableInputFieldGraphType>();
            services.AddSingleton<FilterableInputGraphType>();
            
            return services;
        }
    }
}
