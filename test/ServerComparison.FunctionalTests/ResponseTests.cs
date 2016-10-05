// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    // Uses ports ranging 5080 - 5099.
    public class ResponseTests
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5080/", ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5081/", ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckContentLengthAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5082/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5083/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5084/", ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5085/", ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5087/", ApplicationType.Portable)]
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_Windows_Http10ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckHttp10ConnectionCloseAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5087/", ApplicationType.Portable)] // https://github.com/aspnet/WebListener/issues/259
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_Windows_Http11ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckHttp11ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5088/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5089/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http10ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckHttp10ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5088/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5089/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http11ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckHttp11ConnectionCloseAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5090/", ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5091/", ApplicationType.Portable)]
        public Task ResponseFormats_Windows_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5092/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5093/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5094/", ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5095/", ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5096/", ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5097/", ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5098/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5099/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5100/", ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5101/", ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        // [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5102/", ApplicationType.Portable)] // https://github.com/aspnet/IISIntegration/issues/7
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5103/", ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ManuallyChunkAndClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5104/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5104/", ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunkAndClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        public async Task ResponseFormats(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, Func<HttpClient, ILogger, Task> scenario, ApplicationType applicationType)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("ResponseFormats:{0}:{1}:{2}:{3}", serverType, runtimeFlavor, architecture, applicationType));

            using (logger.BeginScope("ResponseFormatsTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "Responses",
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Running", responseText);
                    }
                    catch (XunitException)
                    {
                        logger.LogWarning(response.ToString());
                        logger.LogWarning(responseText);
                        throw;
                    }

                    await scenario(httpClient, logger);
                }
            }
        }

        private static async Task CheckContentLengthAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("contentlength");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Content Length", responseText);
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Equal("14", GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckHttp11ConnectionCloseAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("connectionclose");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Connnection Close", responseText);
                Assert.True(response.Headers.ConnectionClose, "/connectionclose, closed?");
                Assert.True(response.Headers.TransferEncodingChunked);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckHttp10ConnectionCloseAsync(HttpClient client, ILogger logger)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "connectionclose")
            {
                Version = new Version(1, 0)
            };

            var response = await client.SendAsync(requestMessage);
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Connnection Close", responseText);
                Assert.True(response.Headers.ConnectionClose, "/connectionclose, closed?");
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckChunkedAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("chunked");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/chunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckManuallyChunkedAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("manuallychunked");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Manually Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/manuallychunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckManuallyChunkedAndCloseAsync(HttpClient client, ILogger logger)
        {
            var response = await client.GetAsync("manuallychunkedandclose");
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal("Manually Chunked and Close", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/manuallychunkedandclose, chunked?");
                Assert.True(response.Headers.ConnectionClose, "/manuallychunkedandclose, closed?");
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static string GetContentLength(HttpResponseMessage response)
        {
            // Don't use response.Content.Headers.ContentLength, it will dynamically calculate the value if it can.
            IEnumerable<string> values;
            return response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out values) ? values.FirstOrDefault() : null;
        }
    }
}
