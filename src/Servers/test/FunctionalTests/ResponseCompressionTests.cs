// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests;

public class ResponseCompressionTests : LoggedTest
{
    // NGinx's default min size is 20 bytes
    private static readonly string HelloWorldBody = "Hello World;" + new string('a', 20);

    public ResponseCompressionTests(ITestOutputHelper output) : base(output)
    {
    }

    public static TestMatrix NoCompressionTestVariants
        => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.Nginx, ServerType.HttpSys)
            .WithTfms(Tfm.Default)
            .WithAllHostingModels();

    [ConditionalTheory]
    [MemberData(nameof(NoCompressionTestVariants))]
    public Task ResponseCompression_NoCompression(TestVariant variant)
    {
        return ResponseCompression(variant, CheckNoCompressionAsync, hostCompression: false);
    }

    public static TestMatrix HostCompressionTestVariants
        => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Nginx)
            .WithTfms(Tfm.Default)
            .WithAllHostingModels();

    [ConditionalTheory]
    [MemberData(nameof(HostCompressionTestVariants))]
    public Task ResponseCompression_HostCompression(TestVariant variant)
    {
        return ResponseCompression(variant, CheckHostCompressionAsync, hostCompression: true);
    }

    public static TestMatrix AppCompressionTestVariants
        => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.HttpSys) // No pass-through compression for nginx
            .WithTfms(Tfm.Default)
            .WithAllHostingModels();

    [ConditionalTheory]
    [MemberData(nameof(AppCompressionTestVariants))]
    public Task ResponseCompression_AppCompression(TestVariant variant)
    {
        return ResponseCompression(variant, CheckAppCompressionAsync, hostCompression: false);
    }

    public static TestMatrix HostAndAppCompressionTestVariants
        => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.Nginx, ServerType.HttpSys)
            .WithTfms(Tfm.Default)
            .WithAllHostingModels();

    [ConditionalTheory]
    [MemberData(nameof(HostAndAppCompressionTestVariants))]
    public Task ResponseCompression_AppAndHostCompression(TestVariant variant)
    {
        return ResponseCompression(variant, CheckAppCompressionAsync, hostCompression: true);
    }

    private async Task ResponseCompression(TestVariant variant,
        Func<HttpClient, ILogger, Task> scenario,
        bool hostCompression,
        [CallerMemberName] string testName = null)
    {
        testName = $"{testName}_{variant.Server}_{variant.Tfm}_{variant.Architecture}_{variant.ApplicationType}";
        using (StartLog(out var loggerFactory,
            variant.Server == ServerType.Nginx ? LogLevel.Trace : LogLevel.Debug, // https://github.com/aspnet/ServerTests/issues/144
            testName))
        {
            var logger = loggerFactory.CreateLogger("ResponseCompression");

            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetApplicationPath(),
                EnvironmentName = "ResponseCompression",
            };

            if (variant.Server == ServerType.Nginx)
            {
                deploymentParameters.ServerConfigTemplateContent = hostCompression
                    ? Helpers.GetNginxConfigContent("nginx.conf")
                    : Helpers.GetNginxConfigContent("NoCompression.conf");
            }
            else if (variant.Server == ServerType.IISExpress && !hostCompression)
            {
                var iisDeploymentParameters = new IISDeploymentParameters(deploymentParameters);
                iisDeploymentParameters.ServerConfigActionList.Add(
                    (element, _) =>
                    {
                        var compressionElement = element
                            .RequiredElement("system.webServer")
                            .RequiredElement("httpCompression");

                        compressionElement
                            .RequiredElement("dynamicTypes")
                            .Elements()
                            .SkipLast(1)
                            .Remove();

                        compressionElement
                            .RequiredElement("staticTypes")
                            .Elements()
                            .SkipLast(1)
                            .Remove();
                        // last element in both dynamicTypes and staticTypes disables compression
                        // <add mimeType="*/*" enabled="false" />
                    });
                deploymentParameters = iisDeploymentParameters;
            }

            using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
            Assert.Equal(HelloWorldBody.Length.ToString(CultureInfo.InvariantCulture), GetContentLength(response));
            Assert.Empty(response.Content.Headers.ContentEncoding);
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
            Assert.Single(response.Content.Headers.ContentEncoding);
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
