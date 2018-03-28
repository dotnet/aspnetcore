using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;


namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class StartupTests : LoggedTest
    {
        public StartupTests(ITestOutputHelper output) : base(output)
        {

        }

        [Fact]
        public async Task ExpandEnvironmentVariableInWebConfig()
        {
            var runtimeFlavor = RuntimeFlavor.CoreClr;
            var serverType = ServerType.IISExpress;
            var testName = $"HelloWorld_{runtimeFlavor}";
            var architecture = RuntimeArchitecture.x64;
            var dotnetLocation = $"%USERPROFILE%\\.dotnet\\{architecture.ToString()}\\dotnet.exe";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.1",
                    ApplicationType = ApplicationType.Portable,
                    Configuration =
#if DEBUG
                        "Debug"
#else
                        "Release"
#endif
                };

                // Point to dotnet installed in user profile.
                deploymentParameters.EnvironmentVariables["DotnetPath"] = Environment.ExpandEnvironmentVariables(dotnetLocation); // Path to dotnet.

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", "%DotnetPath%");

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("HelloWorld");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);
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

        [Fact]
        public async Task InvalidProcessPath_ExpectServerError()
        {
            var architecture = RuntimeArchitecture.x64;
            var runtimeFlavor = RuntimeFlavor.CoreClr;
            var serverType = ServerType.IISExpress;
            var testName = $"HelloWorld_{runtimeFlavor}";
            var dotnetLocation = "bogus";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.1",
                    ApplicationType = ApplicationType.Portable,
                    Configuration =
#if DEBUG
                        "Debug"
#else
                        "Release"
#endif
                };

                // Point to dotnet installed in user profile.
                deploymentParameters.EnvironmentVariables["DotnetPath"] = Environment.ExpandEnvironmentVariables(dotnetLocation); // Path to dotnet.

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", "%DotnetPath%");

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("HelloWorld");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                }
            }
        }

#if NETCOREAPP2_0 || NETCOREAPP2_1

        [Fact] // Consistently fails on CI for net461
        public async Task StandaloneApplication_ExpectCorrectPublish() 
        {
            var architecture = RuntimeArchitecture.x64;
            var runtimeFlavor = RuntimeFlavor.CoreClr;
            var serverType = ServerType.IISExpress;
            var testName = $"HelloWorld_{runtimeFlavor}";

            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.1",
                    ApplicationType = ApplicationType.Standalone,
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

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("HelloWorld");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);
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

        [Fact] // Consistently fails on CI for net461
        public async Task StandaloneApplication_AbsolutePathToExe_ExpectCorrectPublish()
        {
            var architecture = RuntimeArchitecture.x64;
            var runtimeFlavor = RuntimeFlavor.CoreClr;
            var serverType = ServerType.IISExpress;
            var testName = $"HelloWorld_{runtimeFlavor}";

            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("AppHostConfig/Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.1",
                    ApplicationType = ApplicationType.Standalone,
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

                    Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", $"{deploymentResult.ContentRoot}\\IISTestSite.exe");

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync("HelloWorld");
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);
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

#elif NET461
#else
#error Target frameworks need to be updated
#endif

    }
}
