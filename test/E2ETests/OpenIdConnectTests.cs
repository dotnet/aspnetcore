using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class OpenIdConnectTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public OpenIdConnectTests(ITestOutputHelper output)
        {
            _loggerFactory = new LoggerFactory()
                .AddXunit(output);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task OpenIdConnect_OnWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationType);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task OpenIdConnect_OnNonWindows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationType);
        }

        // TODO: temporarily disabling x86 tests as dotnet xunit test runner currently does not support 32-bit

        //[ConditionalTheory(Skip = "https://github.com/aspnet/MusicStore/issues/565"), Trait("E2Etests", "E2Etests")]
        //[OSSkipCondition(OperatingSystems.Windows)]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        //public async Task OpenIdConnect_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        //{
        //    await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture);
        //}

        private async Task OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            try
            {
                Console.WriteLine($"Starting OpenIdConnectTestSuite:{serverType}:{runtimeFlavor}:{architecture}:{applicationType}");
                var logger = _loggerFactory.CreateLogger($"OpenIdConnectTestSuite:{serverType}:{runtimeFlavor}:{architecture}:{applicationType}");
                using (logger.BeginScope("OpenIdConnectTestSuite"))
                {
                    var musicStoreDbName = DbUtils.GetUniqueName();

                    var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                    {
                        PublishApplicationBeforeDeployment = true,
                        PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                        TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0",
                        Configuration = Helpers.GetCurrentBuildConfiguration(),
                        ApplicationType = applicationType,
                        EnvironmentName = "OpenIdConnectTesting",
                        UserAdditionalCleanup = parameters =>
                        {
                            DbUtils.DropDatabase(musicStoreDbName, logger);
                        }
                    };


                    deploymentParameters.AdditionalPublishParameters =
                        (applicationType == ApplicationType.Standalone ? $" -r {RuntimeEnvironment.GetRuntimeIdentifier()}" : "")
                        + " /p:PublishForTesting=true";

                    // Override the connection strings using environment based configuration
                    deploymentParameters.EnvironmentVariables
                        .Add(new KeyValuePair<string, string>(
                            MusicStoreConfig.ConnectionStringKey,
                            DbUtils.CreateConnectionString(musicStoreDbName)));

                    using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, _loggerFactory))
                    {
                        var deploymentResult = await deployer.DeployAsync();
                        var httpClientHandler = new HttpClientHandler();
                        var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                        // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                        var response = await RetryHelper.RetryRequest(async () =>
                        {
                            return await httpClient.GetAsync(string.Empty);
                        }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                        Assert.False(response == null, "Response object is null because the client could not " +
                            "connect to the server after multiple retries");

                        var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                        Console.WriteLine("Verifying home page");
                        await validator.VerifyHomePage(response);

                        Console.WriteLine("Verifying login by OpenIdConnect");
                        await validator.LoginWithOpenIdConnect();

                        logger.LogInformation("Variation completed successfully.");
                    }
                }
            }
            finally
            {
                Console.WriteLine("Finished OpenIdConnectTestSuite");
            }
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}