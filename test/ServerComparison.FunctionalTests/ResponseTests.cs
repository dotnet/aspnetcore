// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Testing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    // Uses ports ranging 5070 - 5079.
    public class ResponseTests
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5072/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5073/")]
        public Task ResponseFormats_Windows_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckContentLengthAsync);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5075/")]
        public Task ResponseFormats_Kestrel_ContentLength(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckContentLengthAsync);
        }

        // [ConditionalTheory]
        // [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        // TODO: Not supported [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5071/")]
        // https://github.com/aspnet/Helios/issues/148
        // TODO: Chunks anyways [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5073/")]
        // https://github.com/aspnet/WebListener/issues/113
        public Task ResponseFormats_Windows_ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckConnectionCloseAsync);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5075/")]
        public Task ResponseFormats_Kestrel_ConnectionClose(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckConnectionCloseAsync);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5072/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5073/")]
        public Task ResponseFormats_Windows_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync);
        }

        // [Theory]
        // TODO: Not implemented [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5075/")]
        // https://github.com/aspnet/KestrelHttpServer/issues/97
        public Task ResponseFormats_Kestrel_Chunked(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckChunkedAsync);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5072/")]
        // TODO: Not implemented [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5073/")]
        // https://github.com/aspnet/WebListener/issues/112
        public Task ResponseFormats_Windows_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAsync);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5075/")]
        public Task ResponseFormats_Kestrel_ManuallyChunk(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return ResponseFormats(serverType, runtimeFlavor, architecture, applicationBaseUrl, CheckManuallyChunkedAsync);
        }

        public async Task ResponseFormats(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, Func<HttpClient, ILogger, Task> scenario)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("ResponseFormats:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("ResponseFormatsTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "Responses",
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
                        logger.LogWarning(responseText);
                        throw;
                    }

                    await scenario(httpClient, logger);
                }
            }
        }

        private static async Task CheckContentLengthAsync(HttpClient client, ILogger logger)
        {
            string responseText = string.Empty;
            try
            {
                var response = await client.GetAsync("contentlength");
                responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Content Length", responseText);
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Equal("14", GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckConnectionCloseAsync(HttpClient client, ILogger logger)
        {
            string responseText = string.Empty;
            try
            {
                var response = await client.GetAsync("connectionclose");
                responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Connnection Close", responseText);
                Assert.Null(response.Headers.TransferEncodingChunked);
                Assert.True(response.Headers.ConnectionClose, "/connectionclose, closed?");
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckChunkedAsync(HttpClient client, ILogger logger)
        {
            string responseText = string.Empty;
            try
            {
                var response = await client.GetAsync("chunked");
                responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/chunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
                logger.LogWarning(responseText);
                throw;
            }
        }

        private static async Task CheckManuallyChunkedAsync(HttpClient client, ILogger logger)
        {
            string responseText = string.Empty;
            try
            {
                var response = await client.GetAsync("manuallychunked");
                responseText = await response.Content.ReadAsStringAsync();
                Assert.Equal("Manually Chunked", responseText);
                Assert.True(response.Headers.TransferEncodingChunked, "/manuallychunked, chunked?");
                Assert.Null(response.Headers.ConnectionClose);
                Assert.Null(GetContentLength(response));
            }
            catch (XunitException)
            {
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