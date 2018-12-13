// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
    public class ResponseCompressionTests : LoggedTest
    {
        // NGinx's default min size is 20 bytes
        private static readonly string HelloWorldBody = "Hello World;" + new string('a', 20);

        public ResponseCompressionTests(ITestOutputHelper output) : base(output)
        {
        }

        // IIS Express
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseCompression_IISExpress_NoCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseCompression(ServerType.IISExpress,
                runtimeFlavor,
                RuntimeArchitecture.x64,
                CheckNoCompressionAsync,
                applicationType,
                hostCompression: false,
                hostingModel: hostingModel,
                additionalPublishParameters: additionalPublishParameters);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseCompression_IISExpress_HostCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseCompression(ServerType.IISExpress,
                runtimeFlavor,
                RuntimeArchitecture.x64,
                CheckHostCompressionAsync,
                applicationType,
                hostCompression: true,
                hostingModel: hostingModel,
                additionalPublishParameters: additionalPublishParameters);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseCompression_IISExpress_AppCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseCompression(ServerType.IISExpress,
                runtimeFlavor,
                RuntimeArchitecture.x64,
                CheckAppCompressionAsync,
                applicationType,
                hostCompression: true,
                hostingModel: hostingModel,
                additionalPublishParameters: additionalPublishParameters);
        }


        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        public Task ResponseCompression_IISExpress_AppAndHostCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return ResponseCompression(ServerType.IISExpress,
                runtimeFlavor,
                RuntimeArchitecture.x64,
                CheckAppCompressionAsync,
                applicationType,
                hostCompression: true,
                hostingModel: hostingModel,
                additionalPublishParameters: additionalPublishParameters);
        }

        // WebListener
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseCompression_WebListener_NoCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseCompression(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckNoCompressionAsync, applicationType, hostCompression: false);
        }

        // WebListener doesn't support HostCompression
        // "The archive entry was compressed using an unsupported compression method."

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseCompression_WebListener_AppCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseCompression(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckAppCompressionAsync, applicationType, hostCompression: false);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task ResponseCompression_WebListener_AppAndHostCompression(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return ResponseCompression(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, CheckAppCompressionAsync, applicationType, hostCompression: true);
        }

        // Kestrel
        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Kestrel_NoCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckNoCompressionAsync, applicationType, hostCompression: false);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Kestrel_AppCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckAppCompressionAsync, applicationType, hostCompression: false);
        }

        // Nginx
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Nginx_NoCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckNoCompressionAsync, applicationType, hostCompression: false);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Nginx_HostCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckHostCompressionAsync, applicationType, hostCompression: true);
        }

        [ConditionalTheory(Skip = "No pass-through compression https://github.com/aspnet/BasicMiddleware/issues/123")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Nginx_AppCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckHostCompressionAsync, applicationType, hostCompression: false);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task ResponseCompression_Nginx_AppAndHostCompression(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return ResponseCompression(serverType, runtimeFlavor, architecture, CheckAppCompressionAsync, applicationType, hostCompression: true);
        }

        private async Task ResponseCompression(ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            Func<HttpClient, ILogger, Task> scenario,
            ApplicationType applicationType,
            bool hostCompression,
            [CallerMemberName] string testName = null,
            HostingModel hostingModel = HostingModel.OutOfProcess,
            string additionalPublishParameters = "")
        {
            testName = $"{testName}_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("ResponseCompression");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "ResponseCompression",
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType,
                        hostCompression ? "http.config" : "NoCompression.config",
                        hostCompression ? "nginx.conf" : "NoCompression.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    ApplicationType = applicationType,
                    HostingModel = hostingModel,
                    AdditionalPublishParameters = additionalPublishParameters
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.None };
                    Assert.True(httpClientHandler.SupportsAutomaticDecompression);
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

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

        private static async Task CheckNoCompressionAsync(HttpClient client, ILogger logger)
        {
            logger.LogInformation("Testing /NoAppCompression");
            var request = new HttpRequestMessage(HttpMethod.Get, "NoAppCompression");
            request.Headers.AcceptEncoding.ParseAdd("gzip,deflate");
            var response = await client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal(HelloWorldBody, responseText);
                Assert.Equal(HelloWorldBody.Length.ToString(), GetContentLength(response));
                Assert.Equal(0, response.Content.Headers.ContentEncoding.Count);
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static Task CheckHostCompressionAsync(HttpClient client, ILogger logger)
        {
            return CheckCompressionAsync(client, "NoAppCompression", logger);
        }

        private static Task CheckAppCompressionAsync(HttpClient client, ILogger logger)
        {
            return CheckCompressionAsync(client, "AppCompression", logger);
        }

        private static async Task CheckCompressionAsync(HttpClient client, string url, ILogger logger)
        {
            // Manage the compression manually because HttpClient removes the Content-Encoding header when decompressing.
            logger.LogInformation($"Testing /{url}");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.AcceptEncoding.ParseAdd("gzip,deflate");
            var response = await client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                responseText = await ReadCompressedAsStringAsync(response.Content);
                Assert.Equal(HelloWorldBody, responseText);
                Assert.Equal(1, response.Content.Headers.ContentEncoding.Count);
                Assert.Equal("gzip", response.Content.Headers.ContentEncoding.First());
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
            return response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var values) ? values.FirstOrDefault() : null;
        }

        private static async Task<string> ReadCompressedAsStringAsync(HttpContent content)
        {
            using (var stream = await content.ReadAsStreamAsync())
            using (var compressStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(compressStream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
