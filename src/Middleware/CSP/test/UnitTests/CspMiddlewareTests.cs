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
        public async Task cspHeaderIsSetOnAllResponses(string requestPath)
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
        [InlineData("text/html", true)]
        [InlineData("application/json", false)]
        [InlineData(null, true)]
        public async Task cspHeaderIsSetOnlyOnValidResponses(string contentType, bool headerShouldExist)
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
                var response = await server.CreateRequest("/").AddHeader("content-type", contentType)
                    .SendAsync("GET");

                // Assert
                response.EnsureSuccessStatusCode();
                if (headerShouldExist)
                {
                    Assert.Single(response.Headers);
                    Assert.NotEmpty(response.Headers.GetValues(CspConstants.CspEnforcedHeaderName).FirstOrDefault());
                    Assert.Equal("Test response", await response.Content.ReadAsStringAsync());
                }
                else
                {
                    Assert.Empty(response.Headers);
                }
            }
        }

        [Theory]
        [InlineData("GET", "foo")]
        [InlineData("GET", "application/csp-report")]
        [InlineData("POST", "foo")]
        [InlineData("POST", "application/csp-report")]
        [InlineData("PUT", "foo")]
        [InlineData("PUT", "application/csp-report")]
        [InlineData("HEAD", "foo")]
        [InlineData("HEAD", "application/csp-report")]
        public async void ProcessesMalformedReportRequestsCorrectly(string method, string contentType)
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
                    c.Request.Method = method;
                    c.Request.Path = "/cspreport";
                    c.Request.Headers[HeaderNames.ContentType] = contentType;
                    c.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes("malformed"));
                });

                // Assert
                Assert.Equal(204, context.Response.StatusCode);
                Assert.Empty(new StreamReader(context.Response.Body).ReadToEnd());
            }
        }

        [Fact]
        public async void ProcessesCspReportRequestsCorrectly()
        {
            // Arrange
            var logger = new Mock<ILogger<CspReportingMiddleware>>();

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
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(ILogger<CspReportingMiddleware>), logger.Object);
                });

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
                Assert.Equal(204, context.Response.StatusCode);
                Assert.Empty(new StreamReader(context.Response.Body).ReadToEnd());
                //TODO: ASSERT ON THE LOGGING STATEMENT!
                //logger.Verify(m => m.Log(LogLevel.Information, ""));
            }
        }
    }
}
