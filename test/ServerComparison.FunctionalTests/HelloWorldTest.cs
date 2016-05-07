// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests
{
    // Uses ports ranging 5061 - 5069.
    public class HelloWorldTests
    {
        // Tests disabled on x86 because of https://github.com/aspnet/Hosting/issues/601
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5061/", ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5062/", ApplicationType.Portable)]
        //[InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5063/", ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5064/", ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5065/", ApplicationType.Standalone)]
        public Task HelloWorld_Windows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl, applicationType);
        }

        [Theory]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5066/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5067/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5068/", ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5069/", ApplicationType.Standalone)]
        public Task HelloWorld_Kestrel(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5070/", ApplicationType.Portable)]
        [InlineData(ServerType.Nginx, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5071/", ApplicationType.Standalone)]
        public Task HelloWorld_Nginx(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl, applicationType);
        }

        [ConditionalTheory]
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5072/", ApplicationType.Portable)]
        //[InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5073/", ApplicationType.Portable)]
        public Task HelloWorld_IIS_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl, applicationType);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ApplicationType applicationType)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("HelloWorld:{0}:{1}:{2}:{3}", serverType, runtimeFlavor, architecture, applicationType));

            using (logger.BeginScope("HelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(string.Empty);
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
