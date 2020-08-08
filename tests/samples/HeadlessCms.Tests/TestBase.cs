using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace HeadlessCms.Tests
{
    public partial class TestBase
    {
        private IHost Host { get; set; }

        private async Task<IHost> NewHost()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.UseStartup<Startup>();
                });

            Host = await hostBuilder.StartAsync();

            return Host;
        }

        protected async Task<HttpClient> GetTestClient()
            => (Host ?? await NewHost()).GetTestClient();
    }
}