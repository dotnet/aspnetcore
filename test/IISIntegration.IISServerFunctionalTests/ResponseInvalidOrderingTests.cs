// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public class ResponseInvalidOrderingTests : LoggedTest
    {
        public ResponseInvalidOrderingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Skip = "See https://github.com/aspnet/IISIntegration/issues/424")]
        [InlineData("SetStatusCodeAfterWrite")]
        [InlineData("SetHeaderAfterWrite")]
        public Task ResponseInvalidOrderingTests_ExpectFailure(string path)
        {
            return SetResponseInvalidOperations(RuntimeFlavor.CoreClr, ApplicationType.Portable, path);
        }

        private async Task SetResponseInvalidOperations(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, string path)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"SetResponseInvalidOperations_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SetFeaturesTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "ResponseInvalidOrdering", // Will pick the Start class named 'StartupInvalidOrdering',
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
                        return deploymentResult.HttpClient.GetAsync(path);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal($"Started_{path}Threw_Finished", responseText);
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
