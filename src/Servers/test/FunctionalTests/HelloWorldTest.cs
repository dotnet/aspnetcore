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
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task HelloWorld_WebListener(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.WebListener, runtimeFlavor, RuntimeArchitecture.x64, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V2")]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable, HostingModel.OutOfProcess, "/p:ANCMVersion=V1")]
        public Task HelloWorld_IISExpress(RuntimeFlavor runtimeFlavor, ApplicationType applicationType, HostingModel hostingModel, string additionalPublishParameters)
        {
            return HelloWorld(ServerType.IISExpress, runtimeFlavor, RuntimeArchitecture.x64, applicationType, hostingModel: hostingModel, additionalPublishParameters: additionalPublishParameters);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(RuntimeFlavor.Clr, ApplicationType.Portable)]
        public Task HelloWorld_Kestrel_Clr(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, applicationType);
        }

        [Theory]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task HelloWorld_Kestrel(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.Kestrel, runtimeFlavor, RuntimeArchitecture.x64, applicationType);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Portable)]
        [InlineData(RuntimeFlavor.CoreClr, ApplicationType.Standalone)]
        public Task HelloWorld_Nginx(RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            return HelloWorld(ServerType.Nginx, runtimeFlavor, RuntimeArchitecture.x64, applicationType);
        }


        private async Task HelloWorld(ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            [CallerMemberName] string testName = null,
            HostingModel hostingModel = HostingModel.OutOfProcess,
            string additionalPublishParameters = "")
        {
            testName = $"{testName}_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorld");

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = Helpers.GetConfigContent(serverType, "Http.config", "nginx.conf"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    ApplicationType = applicationType,
                    HostingModel = hostingModel,
                    AdditionalPublishParameters = additionalPublishParameters
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
