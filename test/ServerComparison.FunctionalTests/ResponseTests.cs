// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    public class ResponseTests : LoggedTest
    {
        public ResponseTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckContentLengthAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_Windows_Http10ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckHttp10ConnectionCloseAsync, applicationType);
        }


        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)] // https://github.com/aspnet/WebListener/issues/259
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_Windows_Http11ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckHttp11ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http10ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckHttp10ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http11ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckHttp11ConnectionCloseAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task ResponseFormats_Windows_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckManuallyChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckManuallyChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckManuallyChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        // [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)] // https://github.com/aspnet/IISIntegration/issues/7
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task ResponseFormats_Windows_ManuallyChunkAndClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunkAndClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        private async Task ResponseFormats(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, Func<HttpClient, ILogger, Task> scenario, ApplicationType applicationType, [CallerMemberName] string testName = null)
        {
            testName = $"{testName}_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("ResponseFormats");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "Responses",
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync(string.Empty);
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

                    await scenario(deploymentResult.HttpClient, logger);
                }
            }
        }

        private static async Task CheckContentLengthAsync(HttpClient client, ILogger logger)
        {
            logger.LogInformation("Testing ContentLength");
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "contentlength")
            {
                Version = new Version(1, 1)
            };

            var response = await client.SendAsync(requestMessage);
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
            logger.LogInformation("Testing Http11ConnectionClose");
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
            logger.LogInformation("Testing Http10ConnectionClose");
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
            logger.LogInformation("Testing Chunked");
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "chunked")
            {
                Version = new Version(1, 1)
            };

            var response = await client.SendAsync(requestMessage);
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
            logger.LogInformation("Testing ManuallyChunked");
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "manuallychunked")
            {
                Version = new Version(1, 1)
            };

            var response = await client.SendAsync(requestMessage);
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
            logger.LogInformation("Testing ManuallyChunkedAndClose");
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
