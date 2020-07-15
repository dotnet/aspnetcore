using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class CspMiddlewareTests
    {
        [Fact]
        public async Task test()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCsp(policyBuilder =>
                    {
                        policyBuilder
                            .WithCspMode(CspMode.ENFORCING);
                    });
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Test response");
                    });
                })
                .ConfigureServices(services => services.AddCsp());

            using (var server = new TestServer(hostBuilder))
            {
                // Act
                // Actual request.
                var response = await server.CreateRequest("/")
                    .SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Single(response.Headers);
                var expectedPolicy = "object-src 'none'; script-src 'nonce-{random}' 'strict-dynamic' https: http:; base-uri 'none'; ";
                Assert.Equal(expectedPolicy, response.Headers.GetValues(CspConstants.CspHeaderKey).FirstOrDefault());
                Assert.Equal("Test response", await response.Content.ReadAsStringAsync());
            }
        }
    }
}
