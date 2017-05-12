using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class SmokeTests_X86
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests_X86(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory(Skip = "temporarily disabling these tests as dotnet xunit runner does not support 32-bit yet.")]
        [Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, architecture, applicationType);
        }

        [ConditionalTheory(Skip = "Temporarily disabling test")]
        [Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x86, ApplicationType.Portable)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, architecture, applicationType);
        }
    }

    public class SmokeTests_X64
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests_X64(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, architecture, applicationType);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, architecture, applicationType);
        }
    }

    public class SmokeTests_OnIIS
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests_OnIIS(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory]
        [Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        [InlineData(ServerType.IIS, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IIS, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task SmokeTestSuite_On_IIS_X86(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(
                serverType, architecture, applicationType, noSource: true);
        }
    }

    public class SmokeTests : LoggedTest
    {
        public SmokeTests(ITestOutputHelper output) : base(output)
        {
        }

        public async Task SmokeTestSuite(
            ServerType serverType,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource = false)
        {
            var testName = $"SmokeTestSuite_{serverType}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SmokeTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, RuntimeFlavor.CoreClr, architecture)
                {
                    EnvironmentName = "SocialTesting",
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "MusicStoreTestSite",
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

                    Helpers.SetInMemoryStoreForIIS(deploymentParameters, logger);

                    await SmokeTestHelper.RunTestsAsync(deploymentResult, logger);
                }
            }
        }
    }
}
