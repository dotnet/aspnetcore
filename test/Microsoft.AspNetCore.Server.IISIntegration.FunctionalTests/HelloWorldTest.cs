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

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    // Uses ports ranging 5061 - 5069.
    public class HelloWorldTests
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5061/", ServerType.Kestrel, ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5062/", ServerType.Kestrel, ApplicationType.Standalone)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5063/", ServerType.Kestrel, ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5064/", ServerType.Kestrel, ApplicationType.Standalone)]
        [InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5065/", ServerType.Kestrel, ApplicationType.Portable)]
        public Task HelloWorld_IISExpress(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.IISExpress, runtimeFlavor, architecture, applicationBaseUrl, delegateServer, applicationType);
        }

        [ConditionalTheory]
        [SkipIfIISVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5069/", ServerType.Kestrel, ApplicationType.Standalone)]
        //[InlineData(RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5070/", ServerType.Kestrel, ApplicationType.Standalone)]
        public Task HelloWorld_IIS(RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.IIS, runtimeFlavor, architecture, applicationBaseUrl, delegateServer, applicationType);
        }

        public async Task HelloWorld(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, ServerType delegateServer, ApplicationType applicationType)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger($"HelloWorld:{serverType}:{runtimeFlavor}:{architecture}:{delegateServer}");

            using (logger.BeginScope("HelloWorldTest"))
            {
                var deploymentParameters = new DeploymentParameters(Helpers.GetTestSitesPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    PublishTargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    ApplicationType = applicationType
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
