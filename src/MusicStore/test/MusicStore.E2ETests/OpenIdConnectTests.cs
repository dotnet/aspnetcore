using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "E2Etests")]
    public class OpenIdConnectTests : LoggedTest
    {
        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress, ServerType.Kestrel)
                .WithTfms(Tfm.NetCoreApp50);

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        public async Task OpenIdConnectTestSuite(TestVariant variant)
        {
            var testName = $"OpenIdConnectTestSuite_{variant}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("OpenIdConnectTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
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

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
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
