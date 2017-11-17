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
    [Trait("E2Etests", "E2Etests")]
    public class OpenIdConnectTests : LoggedTest
    {
        public OpenIdConnectTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public Task OpenIdConnect_Kestrel_CoreCLR_Portable()
        {
            return OpenIdConnectTestSuite(ServerType.Kestrel, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        [Fact]
        public Task OpenIdConnect_Kestrel_CoreCLR_Standalone()
        {
            return OpenIdConnectTestSuite(ServerType.Kestrel, RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public Task OpenIdConnect_Kestrel_CLR()
        {
            return OpenIdConnectTestSuite(ServerType.Kestrel, RuntimeFlavor.Clr, ApplicationType.Portable);
        }

        private async Task OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            var architecture = RuntimeArchitecture.x64;
            var testName = $"OpenIdConnectTestSuite_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("OpenIdConnectTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
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
