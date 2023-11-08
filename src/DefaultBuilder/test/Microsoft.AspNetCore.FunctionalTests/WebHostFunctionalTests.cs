// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Tests;

public class WebHostFunctionalTests : LoggedTest
{
    [Fact]
    public async Task Start_RequestDelegate_Url()
    {
        await ExecuteStartOrStartWithTest(deploymentResult => deploymentResult.HttpClient.GetAsync(string.Empty), "StartRequestDelegateUrlApp");
    }

    [Fact]
    public async Task Start_RouteBuilder_Url()
    {
        await ExecuteStartOrStartWithTest(deploymentResult => deploymentResult.HttpClient.GetAsync("/route"), "StartRouteBuilderUrlApp");
    }

    [Fact]
    public async Task StartWith_IApplicationBuilder_Url()
    {
        await ExecuteStartOrStartWithTest(deploymentResult => deploymentResult.HttpClient.GetAsync(string.Empty), "StartWithIApplicationBuilderUrlApp");
    }

    [Fact]
    public async Task CreateDefaultBuilder_InitializeWithDefaults()
    {
        var applicationName = "CreateDefaultBuilderApp";
        await ExecuteTestApp(applicationName, async (deploymentResult, logger) =>
        {
            var response = await RetryHelper.RetryRequest(() => deploymentResult.HttpClient.GetAsync(string.Empty), logger, deploymentResult.HostShutdownToken, retryCount: 5);

            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                // Assert server is Kestrel
                Assert.Equal("Kestrel", response.Headers.Server.ToString());
                // The application name will be sent in response when all asserts succeed in the test app.
                Assert.Equal(applicationName, responseText);
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }, setTestEnvVars: true);
    }

    [Fact]
    public async Task CreateDefaultBuilderOfT_InitializeWithDefaults()
    {
        var applicationName = "CreateDefaultBuilderOfTApp";
        await ExecuteTestApp(applicationName, async (deploymentResult, logger) =>
        {
            var response = await RetryHelper.RetryRequest(() => deploymentResult.HttpClient.GetAsync(string.Empty), logger, deploymentResult.HostShutdownToken, retryCount: 5);

            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                // Assert server is Kestrel
                Assert.Equal("Kestrel", response.Headers.Server.ToString());
                // The application name will be sent in response when all asserts succeed in the test app.
                Assert.Equal(applicationName, responseText);
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }, setTestEnvVars: true);
    }

    [Theory]
    [InlineData("Development", "InvalidOperationException: Cannot consume scoped service")]
    [InlineData("Production", "Success")]
    public async Task CreateDefaultBuilder_InitializesDependencyInjectionSettingsBasedOnEnv(string environment, string expected)
    {
        var applicationName = "DependencyInjectionApp";
        await ExecuteTestApp(applicationName, async (deploymentResult, logger) =>
        {
            var response = await RetryHelper.RetryRequest(() => deploymentResult.HttpClient.GetAsync(string.Empty), logger, deploymentResult.HostShutdownToken);
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                // Assert UseDeveloperExceptionPage is called in WebHostStartupFilter.
                Assert.Contains(expected, responseText);
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        }, setTestEnvVars: true, environment: environment);
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/36079")]
    public void LoggingConfigurationSectionPassedToLoggerByDefault()
    {
        try
        {
            File.WriteAllText("appsettings.json", @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Warning""
        }
    }
}
");
            using (var webHost = WebHost.Start("http://127.0.0.1:0", context => context.Response.WriteAsync("Hello, World!")))
            {
                var factory = (ILoggerFactory)webHost.Services.GetService(typeof(ILoggerFactory));
                var logger = factory.CreateLogger("Test");

                logger.Log(LogLevel.Information, 0, "Message", null, (s, e) =>
                {
                    Assert.True(false, "Information log when log level set to warning in config");
                    return string.Empty;
                });

                var logWritten = false;
                logger.Log(LogLevel.Warning, 0, "Message", null, (s, e) =>
                {
                    logWritten = true;
                    return string.Empty;
                });

                Assert.True(logWritten);
            }
        }
        finally
        {
            File.Delete("appsettings.json");
        }
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public async Task RunsInIISExpressInProcess()
    {
        var applicationName = "CreateDefaultBuilderApp";
        var deploymentParameters = new DeploymentParameters(Path.Combine(GetTestSitesPath(), applicationName), ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
        {
            TargetFramework = "net9.0",
            HostingModel = HostingModel.InProcess
        };

        SetEnvironmentVariables(deploymentParameters, "Development");

        using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, LoggerFactory))
        {
            var deploymentResult = await deployer.DeployAsync();
            var response = await RetryHelper.RetryRequest(() => deploymentResult.HttpClient.GetAsync(string.Empty), Logger, deploymentResult.HostShutdownToken, retryCount: 5);

            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                // Assert server is IISExpress
                Assert.Equal("Microsoft-IIS/10.0", response.Headers.Server.ToString());
                // The application name will be sent in response when all asserts succeed in the test app.
                Assert.Equal(applicationName, responseText);
            }
            catch (XunitException)
            {
                Logger.LogWarning(response.ToString());
                Logger.LogWarning(responseText);
                throw;
            }
        }
    }

    private async Task ExecuteStartOrStartWithTest(Func<DeploymentResult, Task<HttpResponseMessage>> getResponse, string applicationName)
    {
        await ExecuteTestApp(applicationName, async (deploymentResult, logger) =>
        {
            var response = await RetryHelper.RetryRequest(() => getResponse(deploymentResult), logger, deploymentResult.HostShutdownToken);

            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                Assert.Equal(applicationName, responseText);
            }
            catch (XunitException)
            {
                logger.LogWarning(response.ToString());
                logger.LogWarning(responseText);
                throw;
            }
        });
    }

    private async Task ExecuteTestApp(string applicationName,
        Func<DeploymentResult, ILogger, Task> assertAction,
        bool setTestEnvVars = false,
        string environment = "Development")
    {
        var deploymentParameters = new DeploymentParameters(Path.Combine(GetTestSitesPath(), applicationName), ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitectures.Current)
        {
            TargetFramework = "net9.0",
        };

        if (setTestEnvVars)
        {
            SetEnvironmentVariables(deploymentParameters, environment);
        }

        using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, LoggerFactory))
        {
            var deploymentResult = await deployer.DeployAsync();

            await assertAction(deploymentResult, Logger);
        }
    }

    private static void SetEnvironmentVariables(DeploymentParameters deploymentParameters, string environment)
    {
        deploymentParameters.EnvironmentVariables.Add(new KeyValuePair<string, string>("aspnetcore_environment", environment));
        deploymentParameters.EnvironmentVariables.Add(new KeyValuePair<string, string>("envKey", "envValue"));
    }

    private static string GetTestSitesPath()
    {
        var applicationBasePath = AppContext.BaseDirectory;

        var directoryInfo = new DirectoryInfo(applicationBasePath);
        do
        {
            var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "DefaultBuilder.slnf"));
            if (solutionFileInfo.Exists)
            {
                return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "testassets"));
            }

            directoryInfo = directoryInfo.Parent;
        }
        while (directoryInfo.Parent != null);

        throw new Exception($"Solution root could not be found using {applicationBasePath}");
    }
}
