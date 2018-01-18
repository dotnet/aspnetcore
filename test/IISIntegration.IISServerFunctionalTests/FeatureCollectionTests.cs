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
    public class FeatureCollectionTest : LoggedTest
    {
        public FeatureCollectionTest(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        [InlineData("SetRequestFeatures")]
        [InlineData("SetResponseFeatures")]
        [InlineData("SetConnectionFeatures")]
        public Task FeatureCollectionTest_SetHttpContextFeatures(string path)
        {
            return SetFeatures(RuntimeFlavor.CoreClr, ApplicationType.Portable, path);
        }

        private async Task SetFeatures(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, string path)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"SetFeatures_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SetFeaturesTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "FeatureCollection", // Will pick the Start class named 'StartupFeatureCollection',
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
                        return deploymentResult.HttpClient.GetAsync(path + "?query");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Success", responseText);
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
