using GraphQL.Server;
using HeadlessCms.Data;
using HeadlessCms.GraphQL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HeadlessCms
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContext<CmsDbContext>(ServiceLifetime.Transient)
                .AddGraphQLEntityFrameworkCoreHelpers()
                .AddGraphQL()
                .AddSystemTextJson()
                .AddDataLoader()
                .AddGraphTypes();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseGraphQL<CmsSchema>("/graphql");
        }
    }
}
