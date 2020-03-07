// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
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

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(/* ServerType.IISExpress, https://github.com/dotnet/aspnetcore/issues/6168, */ ServerType.Kestrel, ServerType.Nginx, ServerType.HttpSys)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllHostingModels();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task ResponseFormats_ContentLength(TestVariant variant)
        {
            return ResponseFormats(variant, CheckContentLengthAsync);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task ResponseFormats_Chunked(TestVariant variant)
        {
            return ResponseFormats(variant, CheckChunkedAsync);
        }

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public Task ResponseFormats_ManuallyChunk(TestVariant variant)
        {
            return ResponseFormats(variant, CheckManuallyChunkedAsync);
        }

        public static TestMatrix SelfhostTestVariants
            => TestMatrix.ForServers(ServerType.Kestrel, ServerType.HttpSys)
                .WithTfms(Tfm.NetCoreApp50);

        // Connection Close tests do not work through reverse proxies
        [ConditionalTheory]
        [MemberData(nameof(SelfhostTestVariants))]
        public Task ResponseFormats_Http10ConnectionClose(TestVariant variant)
        {
            return ResponseFormats(variant, CheckHttp10ConnectionCloseAsync);
        }

        [ConditionalTheory]
        [MemberData(nameof(SelfhostTestVariants))]
        public Task ResponseFormats_Http11ConnectionClose(TestVariant variant)
        {
            return ResponseFormats(variant, CheckHttp11ConnectionCloseAsync);
        }

        [ConditionalTheory]
        [MemberData(nameof(SelfhostTestVariants))]
        public Task ResponseFormats_ManuallyChunkAndClose(TestVariant variant)
        {
            return ResponseFormats(variant, CheckManuallyChunkedAndCloseAsync);
        }

        private async Task ResponseFormats(TestVariant variant, Func<HttpClient, ILogger, Task> scenario, [CallerMemberName] string testName = null)
        {
            testName = $"{testName}_{variant.Server}_{variant.Tfm}_{variant.Architecture}_{variant.ApplicationType}";
            using (StartLog(out var loggerFactory,
                variant.Server == ServerType.Nginx ? LogLevel.Trace : LogLevel.Debug, // https://github.com/aspnet/ServerTests/issues/144
                testName))
            {
                var logger = loggerFactory.CreateLogger("ResponseFormats");

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    EnvironmentName = "Responses"
                };

                if (variant.Server == ServerType.Nginx)
                {
                    deploymentParameters.ServerConfigTemplateContent = Helpers.GetNginxConfigContent("nginx.conf");
                }

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
