// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ServerComparison.FunctionalTests;

public class HelloWorldTests : LoggedTest
{
    private const string DebugEnvironmentVariable = "ASPNETCORE_MODULE_DEBUG";

    public HelloWorldTests(ITestOutputHelper output) : base(output)
    {
    }

    public static TestMatrix TestVariants
        => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel, ServerType.Nginx, ServerType.HttpSys)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithAllHostingModels()
            .WithAllArchitectures();

    [ConditionalTheory]
    [MemberData(nameof(TestVariants))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/45378")]
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

            if (variant.Server == ServerType.IISExpress)
            {
                deploymentParameters.EnvironmentVariables[DebugEnvironmentVariable] = "console";
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
                    string expectedName = Enum.GetName(typeof(RuntimeArchitecture), variant.Architecture);
                    expectedName = char.ToUpperInvariant(expectedName[0]) + expectedName.Substring(1);
                    Assert.Equal($"Hello World {expectedName}", responseText);
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

    public static TestMatrix SelfHostTestVariants
        => TestMatrix.ForServers(ServerType.Kestrel, ServerType.HttpSys)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithAllHostingModels()
            .WithAllArchitectures();

    [ConditionalTheory]
    [MemberData(nameof(SelfHostTestVariants))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/42380")]
    public async Task ApplicationException(TestVariant variant)
    {
        var testName = $"ApplicationException_{variant.Server}_{variant.Tfm}_{variant.Architecture}_{variant.ApplicationType}";
        using (StartLog(out var loggerFactory, LogLevel.Debug, testName))
        {
            var logger = loggerFactory.CreateLogger("ApplicationException");

            var deploymentParameters = new DeploymentParameters(variant)
            {
                ApplicationPath = Helpers.GetApplicationPath()
            };

            var output = string.Empty;
            using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
            {
                deployer.ProcessOutputListener = (data) =>
                {
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        output += data + '\n';
                    }
                };

                var deploymentResult = await deployer.DeployAsync();

                // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                using (var response = await RetryHelper.RetryRequest(() =>
                {
                    return deploymentResult.HttpClient.GetAsync("/throwexception");
                }, logger, deploymentResult.HostShutdownToken))
                {
                    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

                    var body = await response.Content.ReadAsStringAsync();
                    Assert.Empty(body);
                }
            }
            // Output should contain the ApplicationException and the 500 status code
            Assert.Contains("System.ApplicationException: Application exception", output);
            Assert.Contains("/throwexception - 500", output);
        }
    }
}
