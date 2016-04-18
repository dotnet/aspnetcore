using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // Uses ports ranging 5040 - 5049.
    public class OpenIdConnectTests : IDisposable
    {
        private readonly XunitLogger _logger;

        public OpenIdConnectTests(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5040/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5041/")]
        public async Task OpenIdConnect_OnWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5042/")]
        public async Task OpenIdConnect_OnNonWindows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        // TODO: temporarily disabling x86 tests as dotnet xunit test runner currently does not support 32-bit

        //[ConditionalTheory(Skip = "https://github.com/aspnet/MusicStore/issues/565"), Trait("E2Etests", "E2Etests")]
        //[OSSkipCondition(OperatingSystems.Windows)]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5043/")]
        //public async Task OpenIdConnect_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        //{
        //    await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        //}

        private async Task OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            using (_logger.BeginScope("OpenIdConnectTestSuite"))
            {
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PublishTargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netstandardapp1.5",
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "OpenIdConnectTesting",
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, _logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStore.StoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, _logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: _logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, _logger, deploymentResult);
                    await validator.VerifyHomePage(response);

                    // OpenIdConnect login.
                    await validator.LoginWithOpenIdConnect();

                    _logger.LogInformation("Variation completed successfully.");
                }
            }
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }
}