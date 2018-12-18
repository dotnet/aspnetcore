// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#if NETCOREAPP2_0 || NETCOREAPP2_1

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class HelloWorldTests : LoggedTest
    {
        public HelloWorldTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task HelloWorld_IISExpress_Clr_X64_Portable()
        {
            return HelloWorld(RuntimeFlavor.Clr, ApplicationType.Portable, "V1");
        }

        [Fact]
        public Task HelloWorld_IISExpress_CoreClr_X64_Portable()
        {
            return HelloWorld(RuntimeFlavor.CoreClr, ApplicationType.Portable, "V1");
        }

        private async Task HelloWorld(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, string ancmVersion)
        {
            var serverType = ServerType.IISExpress;
            var architecture = RuntimeArchitecture.x64;
            var testName = $"HelloWorld_{runtimeFlavor}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetOutOfProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1",
                    ApplicationType = applicationType,
                    Configuration =
#if DEBUG
                        "Debug",
#else
                        "Release",
#endif
                    AdditionalPublishParameters = $" /p:ANCMVersion={ancmVersion}"
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Path%3F%3F?query");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("/Path??", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Query%3FPath?query?");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("?query?", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/BodyLimit");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("null", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Auth");
                        responseText = await response.Content.ReadAsStringAsync();

                        // We adapted the Http.config file to be used for inprocess too. We specify WindowsAuth is enabled
                        // We now expect that windows auth is enabled rather than disabled.
                        Assert.True("backcompat;Windows".Equals(responseText) || "latest;Windows".Equals(responseText), "Auth");
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
#elif NET461
#else
#error Target frameworks need to be updated
#endif
