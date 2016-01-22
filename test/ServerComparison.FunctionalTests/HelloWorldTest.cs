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
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5061/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5062/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5063/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5064/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5065/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5066/")]
        public Task HelloWorld_Windows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [Theory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5067/")]
        public Task HelloWorld_Kestrel(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x64, "http://localhost:5069/")]
        public Task HelloWorld_Mono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfIISVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5069/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5070/")]
        public Task HelloWorld_IIS_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("HelloWorld:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("HelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    Command = "web",
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ApplicationHostConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
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