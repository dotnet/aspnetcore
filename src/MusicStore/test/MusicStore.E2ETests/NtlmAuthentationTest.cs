using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    [Trait("E2Etests", "E2Etests")]
    public class NtlmAuthenticationTests : LoggedTest
    {
        public static TestMatrix TestVariants
            => TestMatrix.ForServers(ServerType.IISExpress, ServerType.HttpSys)
                .WithTfms(Tfm.NetCoreApp50)
                .WithAllApplicationTypes()
                .WithAllArchitectures();

        [ConditionalTheory]
        [MemberData(nameof(TestVariants))]
        private async Task NtlmAuthenticationTest(TestVariant variant)
        {
            var testName = $"NtlmAuthentication_{variant}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(variant)
                {
                    ApplicationPath = Helpers.GetApplicationPath(),
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                if (variant.Server == ServerType.IISExpress)
                {
                    var iisDeploymentParameters = new IISDeploymentParameters(deploymentParameters);
                    iisDeploymentParameters.ServerConfigActionList.Add(
                        (element, _) => {
                            var authentication = element
                                .RequiredElement("system.webServer")
                                .GetOrAdd("security")
                                .GetOrAdd("authentication");

                            authentication.GetOrAdd("anonymousAuthentication")
                                .SetAttributeValue("enabled", "false");

                            authentication.GetOrAdd("windowsAuthentication")
                                .SetAttributeValue("enabled", "true");
                        });
                    deploymentParameters = iisDeploymentParameters;
                }

                using (var deployer = IISApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
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
                    await validator.VerifyNtlmHomePage(response);

                    logger.LogInformation("Verifying architecture");
                    validator.VerifyArchitecture(response, deploymentResult.DeploymentParameters.RuntimeArchitecture);

                    logger.LogInformation("Verifying access to store with permissions");
                    await validator.AccessStoreWithPermissions();

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
