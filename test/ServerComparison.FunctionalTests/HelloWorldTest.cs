// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using DeploymentHelpers;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace ServerComparison.FunctionalTests
{
    // Uses ports ranging 5061 - 5069.
    public class HelloWorldTests
    {
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Unix | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5061/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5062/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5063/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5064/")]
        public Task HelloWorld_Windows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [Theory]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5065/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5066/")]
        public Task HelloWorld_Kestrel(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.mono, RuntimeArchitecture.x86, "http://localhost:5067/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.mono, RuntimeArchitecture.x64, "http://localhost:5068/")]
        public Task HelloWorld_Mono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfIISVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IIS, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5069/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5070/")]
        public Task HelloWorld_IIS_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfIISNativeVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5071/")]
        public Task HelloWorld_NativeModule_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            return HelloWorld(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfIISNativeVariationsNotEnabled]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipOn32BitOS]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5072/")]
        public Task HelloWorld_NativeModule_AMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
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
                var stopwatch = Stopwatch.StartNew();

                logger.LogInformation("Variation Details : HostType = {hostType}, RuntimeFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld'
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

                    logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);

                    var responseText = await response.Content.ReadAsStringAsync();
                    Assert.Equal("Hello World", responseText);

                    stopwatch.Stop();
                    logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                }
            }
        }
    }
}