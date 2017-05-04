using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class OpenIdConnectTests : LoggedTest
    {
        public OpenIdConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task OpenIdConnect_OnWindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            await OpenIdConnectTestSuite(serverType, architecture, applicationType);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task OpenIdConnect_OnNonWindows(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            await OpenIdConnectTestSuite(serverType, architecture, applicationType);
        }

        private async Task OpenIdConnectTestSuite(ServerType serverType, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            var testName = $"OpenIdConnectTestSuite_{serverType}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("OpenIdConnectTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = "netcoreapp2.0",
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
                    EnvironmentName = "OpenIdConnectTesting",
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    },
                    AdditionalPublishParameters = " /p:PublishForTesting=true"
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                    logger.LogInformation("Verifying home page");
                    await validator.VerifyHomePage(response);

                    logger.LogInformation("Verifying login by OpenIdConnect");
                    await validator.LoginWithOpenIdConnect();

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
