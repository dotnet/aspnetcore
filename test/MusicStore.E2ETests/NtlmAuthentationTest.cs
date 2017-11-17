using System;
using System.Collections.Generic;
using System.IO;
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
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public class NtlmAuthenticationTests : LoggedTest
    {
        public NtlmAuthenticationTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public Task NtlmAuthenticationTest_WebListener_CoreCLR_Portable()
        {
            return NtlmAuthenticationTest(ServerType.WebListener, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        [ConditionalFact]
        public Task NtlmAuthenticationTest_WebListener_CoreCLR_Standalone()
        {
            return NtlmAuthenticationTest(ServerType.WebListener, RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

        [ConditionalFact]
        public Task NtlmAuthenticationTest_IISExpress_CoreCLR_Portable()
        {
            return NtlmAuthenticationTest(ServerType.IISExpress, RuntimeFlavor.CoreClr, ApplicationType.Portable);
        }

        [ConditionalFact]
        public Task NtlmAuthenticationTest_IISExpress_CoreCLR_Standalone()
        {
            return NtlmAuthenticationTest(ServerType.IISExpress, RuntimeFlavor.CoreClr, ApplicationType.Standalone);
        }

#if NETCOREAPP2_1 // Avoid running CLR based tests once on netcoreapp2.0 and netcoreapp2.1 each
        [ConditionalFact]
        public Task NtlmAuthenticationTest_WebListener_CLR()
        {
            return NtlmAuthenticationTest(ServerType.WebListener, RuntimeFlavor.Clr, ApplicationType.Portable);
        }

        [ConditionalFact]
        public Task NtlmAuthenticationTest_IISExpress_CLR()
        {
            return NtlmAuthenticationTest(ServerType.IISExpress, RuntimeFlavor.Clr, ApplicationType.Standalone);
        }
#endif

        private async Task NtlmAuthenticationTest(ServerType serverType, RuntimeFlavor runtimeFlavor, ApplicationType applicationType)
        {
            var architecture = RuntimeArchitecture.x64;
            var testName = $"NtlmAuthentication_{serverType}_{runtimeFlavor}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("NtlmAuthenticationTest");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = Helpers.GetTargetFramework(runtimeFlavor),
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
                    EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NtlmAuthentation.config")) : null,
                    SiteName = "MusicStoreNtlmAuthentication", //This is configured in the NtlmAuthentication.config
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

                    logger.LogInformation("Verifying access to store with permissions");
                    await validator.AccessStoreWithPermissions();

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
