// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class LargeResponseBodyTests : LoggedTest
    {

        public LargeResponseBodyTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        [InlineData(10000)]
        [InlineData(100000)]
        [InlineData(1000000)]
        [InlineData(10000000)]
        public Task LargeResponseBodyTestCheckAllResponseBodyBytesWritten(int query)
        {
            return LargeResponseBody(RuntimeFlavor.CoreClr, ApplicationType.Portable, query);
        }

        private async Task LargeResponseBody(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, int query)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"SetFeatures_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SetFeaturesTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "LargeResponseBody", // Will pick the Start class named 'StartupLargeResponseBody',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.0",
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
                        return deploymentResult.HttpClient.GetAsync("LargeResponseBody?length=" + query);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal(new string('a', query), responseText);
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


        [Fact (Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        public Task LargeFileResponseBodyInternalCheck()
        {
            return LargeResponseBodyFromFile(RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        private async Task LargeResponseBodyFromFile(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"SetFeatures_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SetFeaturesTest");
                var expected = File.ReadAllText("Http.config");
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "LargeResponseBody", // Will pick the Start class named 'StartupLargeResponseBody',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? expected : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.0",
                    ApplicationType = applicationType,

                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    deploymentResult.HttpClient.Timeout = TimeSpan.FromSeconds(5);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("LargeResponseBodyFromFile");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal(expected, responseText);
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
