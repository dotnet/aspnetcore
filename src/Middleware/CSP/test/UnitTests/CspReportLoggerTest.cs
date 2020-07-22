using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Csp.Test.TestUtils;

namespace Microsoft.AspNetCore.Csp.Test
{
    public class CspReportLoggerTest
    {
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
            var testLogger = new CspTestLogger();
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
                .ConfigureServices(services => {
                    services.AddCsp();
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
                testLogger.NoLogStatementsMade();
            }
        }

        [Fact]
        public async void ProcessesCspReportRequestsCorrectly()
        {
            // Arrange
            var mockLogger = new Mock<CspReportLogger>();
            var loggerFactory = new FakeReportLoggerFactory(mockLogger.Object);

            var hostBuilder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseCsp(policyBuilder =>
                    {
                        policyBuilder
                            .WithCspMode(CspMode.ENFORCING)
                            .WithLogLevel(LogLevel.Trace)
                            .WithReportingUri("/cspreport");
                    });
                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Test response");
                    });
                })
                .ConfigureServices(services => {
                    services.AddCsp();
                    services.AddSingleton<ICspReportLoggerFactory>(loggerFactory);
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
                mockLogger.Verify(m => m.Log(It.IsNotNull<Stream>()));
            }
        }

        [Theory]
        [ClassData(typeof(ReportLoggerTestData))]
        public void LogsTextualRepresentationOfReportWhenLoggingLevelAboveOrEqualToInformation(string expectedDescription, string jsonReport, LogLevel logLevel)
        {
            // Arrange
            var testLogger = new CspTestLogger();
            var factory = new CspReportLoggerFactory(testLogger);
            var logger = factory.BuildLogger(logLevel, "reportUri");

            // Act
            logger.Log(new MemoryStream(Encoding.ASCII.GetBytes(jsonReport)));

            // Assert
            testLogger.SingleLogStatementMatching(logLevel, expectedDescription);
        }


        [Theory]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Trace)]
        public void LogsFullRepresentationOfReportWhenLoggingLevelBelowOrEqualToDebug(LogLevel logLevel)
        {
            // Arrange
            var testLogger = new CspTestLogger();
            var factory = new CspReportLoggerFactory(testLogger);
            var logger = factory.BuildLogger(logLevel, "reportUri");
            var jsonReport = @"{
                ""csp-report"": {
                    ""document-uri"": ""http://documenturi"",
                    ""referrer"": ""http://referrer"",
                    ""blocked-uri"": ""http://cdn/script.js"",
                    ""source-file"": ""source.js"",
                    ""line-number"": ""123"",
                    ""violated-directive"": ""script-src-elem"",
                    ""original-policy"": ""script-src 'nonce-abc' http: https:; report-uri /_/csp-reports"",
                    ""disposition"": ""report""
                }
            }";

            // Act
            logger.Log(new MemoryStream(Encoding.ASCII.GetBytes(jsonReport)));

            // Assert
            testLogger.SingleLogStatementMatching(logLevel, jsonReport);
        }
    }

    class ReportLoggerTestData : IEnumerable<object[]>
    {
        List<LogLevel> logLevels = new List<LogLevel>
        {
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Error,
            LogLevel.Critical
        };

        List<string> reports = new List<string>
        {
            // script block missing nonce
            @"{
                ""csp-report"": {
                    ""document-uri"": ""http://documenturi"",
                    ""referrer"": ""http://referrer"",
                    ""blocked-uri"": ""http://cdn/script.js"",
                    ""source-file"": ""source.js"",
                    ""line-number"": ""123"",
                    ""violated-directive"": ""script-src-elem"",
                    ""original-policy"": ""script-src 'nonce-abc' http: https:; report-uri /_/csp-reports"",
                    ""disposition"": ""report""
                }
            }",
            // javascript: URI sample
            @"{
                ""csp-report"": {
                    ""document-uri"": ""http://documenturi"",
                    ""referrer"": ""http://referrer"",
                    ""blocked-uri"": ""inline"",
                    ""violated-directive"": ""script-src-elem"",
                    ""original-policy"": ""script-src 'nonce-abc' http: https:; report-uri /_/csp-reports"",
                    ""disposition"": ""report""
                }
            }",
            // inline javascript (event handler)
            @"{
                ""csp-report"": {
                    ""document-uri"": ""http://documenturi"",
                    ""referrer"": ""http://referrer"",
                    ""blocked-uri"": ""inline"",
                    ""line-number"": ""123"",
                    ""violated-directive"": ""script-src-attr"",
                    ""script-sample"": ""const a = 1"",
                    ""original-policy"": ""script-src 'nonce-abc' http: https:; report-uri /_/csp-reports"",
                    ""disposition"": ""report""
                }
            }",
        };

        List<string> expectedDescriptions = new List<string>
        {
            "Script at http://documenturi (line 123) trying to load http://cdn/script.js refused to run due to missing or mismatching nonce value",
            "Attempt to navigate to javascript URI from http://documenturi was refused by policy",
            "Inline event handler at http://documenturi (line number 123) was refused by policy"
        };
        public IEnumerator<object[]> GetEnumerator()
        {
            return expectedDescriptions
                // match reports and expected descriptions
                .Zip(reports, (d, r) => new { d, r })
                // find all combinations of the previous with each log level
                .SelectMany(reportAndDesc => logLevels.Select(l => new object[] { reportAndDesc.d, reportAndDesc.r, l }))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
