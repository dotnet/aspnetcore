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

namespace Microsoft.AspNetCore.IISPlatformHandler.FunctionalTests
{
    // Uses ports ranging 5061 - 5069.
    public class HelloWorldTests
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5061/", ServerType.Kestrel)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5062/", ServerType.Kestrel)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5063/", ServerType.Kestrel)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5064/", ServerType.Kestrel)]
        public Task HelloWorld_IISExpress(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer)
        {
            return HelloWorld(ServerType.IISExpress, runtimeFlavor, architecture, applicationBaseUrl, delegateServer);
        }

        [ConditionalTheory]
        [SkipIfIISVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5069/", ServerType.Kestrel)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5070/", ServerType.Kestrel)]
        public Task HelloWorld_IIS(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer)
        {
            return HelloWorld(ServerType.IIS, runtimeFlavor, architecture, applicationBaseUrl, delegateServer);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger($"HelloWorld:{serverType}:{runtimeFlavor}:{architecture}:{delegateServer}");

            using (logger.BeginScope("HelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ApplicationHostConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
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
