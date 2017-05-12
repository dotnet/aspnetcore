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
    public class PublishAndRunTests_OnX64
    {
        private readonly ITestOutputHelper _output;

        public PublishAndRunTests_OnX64(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Standalone, false)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone, false)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_output);
            await testRunner.Publish_And_Run_Tests(
                serverType, architecture, applicationType, noSource);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone, false)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_output);
            await testRunner.Publish_And_Run_Tests(
                serverType, architecture, applicationType, noSource);
        }
    }

    public class PublishAndRunTests_OnX86
    {
        private const string SkipReason = "temporarily disabling x86 tests as dotnet xunit test runner currently does not support 32-bit";
        private readonly ITestOutputHelper _output;

        public PublishAndRunTests_OnX86(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory(Skip = SkipReason)]
        [Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x86, ApplicationType.Standalone, false)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Standalone, false)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_output);
            await testRunner.Publish_And_Run_Tests(
                serverType, architecture, applicationType, noSource);
        }

        [ConditionalTheory(Skip = SkipReason)]
        [Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_output);
            await testRunner.Publish_And_Run_Tests(
                serverType, architecture, applicationType, noSource);
        }
    }

    public class PublishAndRunTests : LoggedTest
    {
        public PublishAndRunTests(ITestOutputHelper output) : base(output)
        {
        }

        public async Task Publish_And_Run_Tests(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var noSourceStr = noSource ? "NoSource" : "WithSource";
            var testName = $"PublishAndRunTests_{serverType}_{architecture}_{applicationType}_{noSourceStr}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("Publish_And_Run_Tests");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = "netcoreapp2.0",
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
                    var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                    var httpClient = deploymentResult.CreateHttpClient(httpClientHandler);

                    // Request to base address and check if various parts of the body are rendered &
                    // measure the cold startup time.
                    // Add retry logic since tests are flaky on mono due to connection issues
                    var response = await RetryHelper.RetryRequest(async () => await httpClient.GetAsync(string.Empty), logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

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
