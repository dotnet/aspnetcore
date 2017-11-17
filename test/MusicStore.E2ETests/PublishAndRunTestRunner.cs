using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class PublishAndRunTestRunner : LoggedTest
    {
        public PublishAndRunTestRunner(ITestOutputHelper output) 
            : base(output)
        {
        }

        public async Task RunTests(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            ApplicationType applicationType,
            RuntimeArchitecture runtimeArchitecture)
        {
            var testName = $"PublishAndRunTests_{serverType}_{runtimeFlavor}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("Publish_And_Run_Tests");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, runtimeArchitecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
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

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();
                    var httpClientHandler = new HttpClientHandler { UseDefaultCredentials = true };
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                    // Request to base address and check if various parts of the body are rendered &
                    // measure the cold startup time.
                    // Add retry logic since tests are flaky on mono due to connection issues
                    var response = await RetryHelper.RetryRequest(() => httpClient.GetAsync(string.Empty), logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                    logger.LogInformation("Verifying home page");
                    await validator.VerifyHomePage(response);

                    logger.LogInformation("Verifying static files are served from static file middleware");
                    await validator.VerifyStaticContentServed();

                    if (serverType != ServerType.IISExpress)
                    {
                        if (Directory.GetFiles(
                            deploymentParameters.ApplicationPath, "*.cmd", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            throw new Exception("publishExclude parameter values are not honored.");
                        }
                    }

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
