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

        // IIS Express
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable, HostingModel.OutOfProcess, "", Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseFormats_IISExpress_ContentLength(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseFormats(ServerType.IISExpress, runtimeFlavor, RuntimeArchitecture.x64, CheckContentLengthAsync, applicationType, hostingModel: hostingModel, additionalPublishParameters: additionalPublishParameters);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable, HostingModel.OutOfProcess, "", Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseFormats_IISExpress_Chunked(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseFormats(ServerType.IISExpress, runtimeFlavor, RuntimeArchitecture.x64, CheckChunkedAsync, applicationType, hostingModel: hostingModel, additionalPublishParameters: additionalPublishParameters);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable, HostingModel.OutOfProcess, "", Skip = "Websdk issue with full framework publish. See https://github.com/aspnet/websdk/pull/322")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseFormats_IIS_ManuallyChunk(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseFormats(ServerType.IISExpress, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAsync, applicationType, hostingModel: hostingModel, additionalPublishParameters: additionalPublishParameters);
        }

        // Weblistener
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_WebListener_ContentLength(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_WebListener_Chunked(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_WebListener_Http10ConnectionClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckHttp10ConnectionCloseAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)] // https://github.com/aspnet/WebListener/issues/259
        // IIS will remove the "Connection: close" header https://github.com/aspnet/IISIntegration/issues/7
        public Task ResponseFormats_WebListener_Http11ConnectionClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckHttp11ConnectionCloseAsync, applicationType);
        }


        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_WebListener_ManuallyChunk(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_WebListener_ManuallyChunkAndClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        // Kestrel
        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ContentLength(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckContentLengthAsync, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http10ConnectionClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckHttp10ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Http11ConnectionClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckHttp11ConnectionCloseAsync, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_Chunked(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunk(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAsync, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Kestrel_ManuallyChunkAndClose(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAndCloseAsync, applicationType);
        }

        // Nginx
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ContentLength( RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Nginx, runtimeFlavor, RuntimeArchitecture.x64, CheckContentLengthAsync, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_Chunked(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Nginx, runtimeFlavor, RuntimeArchitecture.x64, CheckChunkedAsync, applicationType);
        }



        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseFormats_Nginx_ManuallyChunk(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseFormats(ServerType.Nginx, runtimeFlavor, RuntimeArchitecture.x64, CheckManuallyChunkedAsync, applicationType);
        }

        private async Task ResponseFormats(ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            Func<HttpClient, ILogger, Task> scenario,
            ApplicationType applicationType,
            [CallerMemberName] string testName = null,
            HostingModel hostingModel = HostingModel.OutOfProcess,
            string additionalPublishParameters = "")
        {
            testName = $"{testName}_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("ResponseFormats");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "Responses",
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    ApplicationType = applicationType,
                    HostingModel = hostingModel,
                    AdditionalPublishParameters = additionalPublishParameters
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
