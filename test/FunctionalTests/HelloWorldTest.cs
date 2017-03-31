// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class HelloWorldTests
    {
        private readonly ITestOutputHelper _output;

        public HelloWorldTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        //[InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task HelloWorld_IISExpress(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.IISExpress, runtimeFlavor, architecture, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        // TODO reenable when https://github.com/dotnet/sdk/issues/696 is resolved
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        public Task HelloWorld_IISExpress_CoreClr(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.IISExpress, runtimeFlavor, architecture, applicationType);
        }

        [ConditionalTheory]
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        public Task HelloWorld_IIS(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.IIS, runtimeFlavor, architecture, applicationType);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            var loggerFactory = new LoggerFactory()
                .AddXunit(_output)
                .AddDebug();
            var logger = loggerFactory.CreateLogger($"HelloWorld:{serverType}:{runtimeFlavor}:{architecture}");

            using (logger.BeginScope("HelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler)
                    {
                        BaseAddress = new Uri(deploymentResult.ApplicationBaseUri),
                        Timeout = TimeSpan.FromSeconds(5),
                    };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);

                        response = await httpClient.GetAsync("/Path%3F%3F?query");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("/Path??", responseText);

                        response = await httpClient.GetAsync("/Query%3FPath?query?");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("?query?", responseText);
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
