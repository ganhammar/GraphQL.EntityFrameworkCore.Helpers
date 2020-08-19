using GraphQL.EntityFrameworkCore.Helpers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLEntityFrameworkCoreHelpers(this IServiceCollection services)
        {
            services.AddSingleton<FilterableModesGraphType>();
            services.AddSingleton<FilterableFieldOperatorsGraphType>();
            services.AddSingleton<FilterableValueOperatorsGraphType>();
            services.AddSingleton<FilterableInputFieldGraphType>();
            services.AddSingleton<FilterableInputGraphType>();
            
            return services;
        }
    }
}
