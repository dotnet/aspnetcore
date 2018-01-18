// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class ResponseHeaders : LoggedTest
    {
        public ResponseHeaders(ITestOutputHelper output) : base(output)
        {
        }

        [Fact(Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        public Task AddResponseHeaders_HeaderValuesAreSetCorrectly()
        {
            return RunResponseHeaders(ApplicationType.Portable);
        }

        private async Task RunResponseHeaders(ApplicationType applicationType)
        {
            var runtimeFlavor = RuntimeFlavor.CoreClr;
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"ResponseHeaders_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "ResponseHeaders", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = applicationType,
                    Configuration =
#if DEBUG
                        "Debug"
#else
                        "Release"
#endif
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    deploymentResult.HttpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("/ResponseHeaders");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal("Request Complete", responseText);

                        Assert.True(response.Headers.TryGetValues("UnknownHeader", out var headerValues));
                        Assert.Equal("test123=foo", headerValues.First());

                        Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentType, out headerValues));
                        Assert.Equal("text/plain", headerValues.First());

                        Assert.True(response.Headers.TryGetValues("MultiHeader", out headerValues));
                        Assert.Equal(2, headerValues.Count());
                        Assert.Equal("1", headerValues.First());
                        Assert.Equal("2", headerValues.Last());
                    }
                    catch (XunitException)
                    {
                        logger.LogWarning(response.ToString());
                        logger.LogWarning(responseText);
                        throw;
                    }
                }
            }
        }
    }
}
