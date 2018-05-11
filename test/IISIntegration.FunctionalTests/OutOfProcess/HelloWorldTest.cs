// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class HelloWorldTests : LoggedTest
    {
        public HelloWorldTests(ITestOutputHelper output) : base(output)
        {
        }

        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress)
                .WithTfms(Tfm.NetCoreApp22, Tfm.Net461)
                .WithAllApplicationTypes()
                .WithAllAncmVersions();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task HelloWorld(TestVariant variant)
        {
            var testName = $"HelloWorld_{variant.Tfm}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("HelloWorldTest");

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetOutOfProcessTestSitesPath(),
                    EnvironmentName = "HelloWorld", // Will pick the Start class named 'StartupHelloWorld',
                    ServerConfigTemplateContent = File.ReadAllText("AppHostConfig/Http.config"),
                    SiteName = "HttpTestSite", // This is configured in the Http.config
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return deploymentResult.HttpClient.GetAsync(string.Empty);
                    }, logger, deploymentResult.HostShutdownToken, retryCount: 30);

                    var responseText = await response.Content.ReadAsStringAsync();
                    try
                    {
                        Assert.Equal("Hello World", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Path%3F%3F?query");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("/Path??", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Query%3FPath?query?");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("?query?", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/BodyLimit");
                        responseText = await response.Content.ReadAsStringAsync();
                        Assert.Equal("null", responseText);

                        response = await deploymentResult.HttpClient.GetAsync("/Auth");
                        responseText = await response.Content.ReadAsStringAsync();

                        // We adapted the Http.config file to be used for inprocess too. We specify WindowsAuth is enabled
                        // We now expect that windows auth is enabled rather than disabled.
                        Assert.True("backcompat;Windows".Equals(responseText) || "latest;Windows".Equals(responseText), "Auth");
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
