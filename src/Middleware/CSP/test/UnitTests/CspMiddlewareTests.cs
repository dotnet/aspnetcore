using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Text;

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
                });

            using (var server = new TestServer(hostBuilder))
            {
                // Act
                var response = await server.CreateRequest("/")
                    .SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Single(response.Headers);
                Assert.NotEmpty(response.Headers.GetValues(CspConstants.CspEnforcedHeaderName).FirstOrDefault());
                Assert.Equal("Test response", await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void ProcessesMalformedReportRequestsCorrectly()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCsp(policyBuilder =>
                    {
                        policyBuilder
                            .WithCspMode(CspMode.ENFORCING)
                            .WithReportingUri("/cspreport");
                    });
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Test response");
                    });
                });

            using (var server = new TestServer(hostBuilder))
            {
                // Act
                var context = await server.SendAsync(c =>
                {
                    c.Request.Method = "POST";
                    c.Request.Path = "/cspreport";
                    c.Request.Headers[HeaderNames.ContentType] = CspConstants.CspReportContentType;
                    c.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes("malformed"));
                });

                // Assert
                Assert.Equal(204, context.Response.StatusCode);
                Assert.Empty(new StreamReader(context.Response.Body).ReadToEnd());
            }
        }
    }
}
