// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.CommandLineUtils;
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
#if NET461
            // use the dotnet on PATH
            var dotnetLocation = "dotnet";
#else
            var dotnetLocation = DotNetMuxer.MuxerPathOrDefault();
#endif
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = GetBaseDeploymentParameters();

                // Point to dotnet installed in user profile.
                deploymentParameters.EnvironmentVariables["DotnetPath"] = dotnetLocation;

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
            var dotnetLocation = "bogus";
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");
                var deploymentParameters = GetBaseDeploymentParameters();

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

#if NETCOREAPP2_1

        [Fact] // Consistently fails on CI for net461
        public async Task StandaloneApplication_ExpectCorrectPublish()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = GetBaseDeploymentParameters();
                deploymentParameters.ApplicationType = ApplicationType.Standalone;

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
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = GetBaseDeploymentParameters();
                deploymentParameters.ApplicationType = ApplicationType.Standalone;

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    Helpers.ModifyAspNetCoreSectionInWebConfig(deploymentResult, "processPath", $"{deploymentResult.ContentRoot}\\InProcessWebSite.exe");

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

        [Fact]
        public async Task DetectsOveriddenServer()
        {
            var testSink = new TestSink();
            using (StartLog(out var loggerFactory))
            {
                var testLoggerFactory = new TestLoggerFactory(testSink, true);
                loggerFactory.AddProvider(new TestLoggerProvider(testLoggerFactory));

                using (var deployer = ApplicationDeployerFactory.Create(GetBaseDeploymentParameters("OverriddenServerWebSite"), loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var response = await deploymentResult.HttpClient.GetAsync("/");
                    Assert.False(response.IsSuccessStatusCode);
                }
            }
            Assert.Contains(testSink.Writes, context => context.State.ToString().Contains("Application is running inside IIS process but is not configured to use IIS server"));
        }

        private DeploymentParameters GetBaseDeploymentParameters(string site = null)
        {
            return new DeploymentParameters(Helpers.GetTestWebSitePath(site ?? "InProcessWebSite"), ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                ServerConfigTemplateContent = File.ReadAllText("AppHostConfig/Http.config"),
                SiteName = "HttpTestSite", // This is configured in the Http.config
                TargetFramework = "netcoreapp2.1",
                ApplicationType = ApplicationType.Portable,
                ANCMVersion = ANCMVersion.AspNetCoreModuleV2
            };
        }

        private class TestLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerFactory _loggerFactory;

            public TestLoggerProvider(TestLoggerFactory loggerFactory)
            {
                _loggerFactory = loggerFactory;
            }

            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return _loggerFactory.CreateLogger(categoryName);
            }
        }
    }
}
