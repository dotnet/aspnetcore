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
using Moq;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class CspMiddlewareTests
    {
        [Theory]
        [InlineData("/")]
        [InlineData("/cheese")]
        [InlineData("/foo")]
        public async Task CspHeaderIsSetOnAllResponses(string requestPath)
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(services => services.AddCsp())
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
                var response = await server.CreateRequest(requestPath)
                    .SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Single(response.Headers);
                Assert.NotEmpty(response.Headers.GetValues(CspConstants.CspEnforcedHeaderName).FirstOrDefault());
                Assert.Equal("Test response", await response.Content.ReadAsStringAsync());
            }
        }

        [Theory]
        [InlineData("/")]
        public async Task CspNonceExistsInHeader(string requestPath)
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(services => services.AddCsp())
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
                var response = await server.CreateRequest(requestPath)
                    .SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal("Test response", await response.Content.ReadAsStringAsync());

                Assert.Single(response.Headers);
                var header = response.Headers.GetValues(CspConstants.CspEnforcedHeaderName).FirstOrDefault();
                Assert.NotEmpty(header);
                Assert.Matches("'nonce-", header);
            }
        }

        [Fact]
        public async void DoesNotCollectReportsIfReportingUriIsNotRelative()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder()
                .ConfigureServices(services => services.AddCsp())
                .Configure(app =>
                {
                    app.UseCsp(policyBuilder =>
                    {
                        policyBuilder
                            .WithCspMode(CspMode.ENFORCING)
                            .WithReportingUri("https://cheese.com/cspreport");
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
                var context = await server.SendAsync(c =>
                {
                    c.Request.Method = "POST";
                    c.Request.Path = "/cspreport";
                    c.Request.Headers[HeaderNames.ContentType] = CspConstants.CspReportContentType;
                    c.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes(
                        @"{
                          ""csp-report"": {
                            ""document-uri"": ""http://example.com/signup.html"",
                            ""referrer"": ""http://evil.com"",
                            ""blocked-uri"": ""http://example.com/css/style.css"",
                            ""violated-directive"": ""style-src cdn.example.com"",
                            ""original-policy"": ""default-src 'none'; style-src cdn.example.com; report-uri /_/csp-reports"",
                            ""disposition"": ""report""
                          }
                        }"
                    ));
                });

                // Assert
                Assert.NotEqual(204, context.Response.StatusCode);
                Assert.NotEmpty(new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEnd());
            }
        }
    }

    public class FakeReportLoggerFactory : ICspReportLoggerFactory
    {
        readonly CspReportLogger logger;
        public FakeReportLoggerFactory(CspReportLogger logger)
        {
            this.logger = logger;
        }
        public CspReportLogger BuildLogger(LogLevel logLevel, string reportUri)
        {
            return logger;
        }
    }
}
