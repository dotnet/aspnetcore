// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    public class HelloWorldTests : LoggedTest
    {
        public HelloWorldTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x86, ApplicationType.Portable, Skip = "https://github.com/aspnet/Hosting/issues/601")]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x86, ApplicationType.Portable, Skip = "https://github.com/aspnet/Hosting/issues/601")]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task HelloWorld_Windows(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(serverType, architecture, applicationType);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Portable, Skip = "https://github.com/aspnet/Hosting/issues/601")]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Standalone, Skip = "https://github.com/aspnet/Hosting/issues/601")]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task HelloWorld_Kestrel(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(serverType, architecture, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public Task HelloWorld_Nginx(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(serverType, architecture, applicationType);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType, [CallerMemberName] string testName = null)
        {
            testName = $"{testName}_{serverType}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorld");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = "netcoreapp2.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
    }
}
