// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
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

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.Nginx, ServerType.HttpSys)
                .WithTfms(Tfm.NetCoreApp50)
                .WithApplicationTypes(ApplicationType.Portable)
                .WithAllHostingModels()
                .WithAllArchitectures();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HelloWorld(TestVariant variant)
        {
            var testName = $"HelloWorld_{variant.Server}_{variant.Tfm}_{variant.Architecture}_{variant.ApplicationType}";
            using (StartLog(out var loggerFactory,
                variant.Server == ServerType.Nginx ? LogLevel.Trace : LogLevel.Debug, // https://github.com/aspnet/ServerTests/issues/144
                testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorld");

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath()
                };

                if (variant.Server == ServerType.Nginx)
                {
                    deploymentParameters.ServerConfigTemplateContent = Helpers.GetNginxConfigContent("nginx.conf");
                }

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
                        if (variant.Architecture == RuntimeArchitecture.x64)
                        {
                            Assert.Equal("Hello World X64", responseText);
                        }
                        else
                        {
                            Assert.Equal("Hello World X86", responseText);
                        }
                    }
                    catch (XunitException)
                    {
                        logger.LogWarning(response.ToString());
                        logger.LogWarning(responseText);
                        throw;
                    }

                    // Make sure it was the right server.
                    var serverHeader = response.Headers.Server.ToString();
                    switch (variant.Server)
                    {
                        case ServerType.HttpSys:
                            Assert.Equal("Microsoft-HTTPAPI/2.0", serverHeader);
                            break;
                        case ServerType.Nginx:
                            Assert.StartsWith("nginx/", serverHeader);
                            break;
                        case ServerType.Kestrel:
                            Assert.Equal("Kestrel", serverHeader);
                            break;
                        case ServerType.IIS:
                        case ServerType.IISExpress:
                            if (variant.HostingModel == HostingModel.OutOfProcess)
                            {
                                Assert.Equal("Kestrel", serverHeader);
                            }
                            else
                            {
                                Assert.StartsWith("Microsoft-IIS/", serverHeader);
                            }
                            break;
                        default:
                            throw new NotImplementedException(variant.Server.ToString());
                    }
                }
            }
        }
    }
}
