using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.xunit;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.DotNet.PlatformAbstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // Uses ports ranging 5001 - 5025.
    // TODO: temporarily disabling these tests as dotnet xunit runner does not support 32-bit yet.
    internal class SmokeTests_X86
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests_X86(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType);
        }

        [ConditionalTheory(Skip = "Temporarily disabling test"), Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType);
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
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType);
        }
    }

    class SmokeTests_OnIIS
    {
        private readonly ITestOutputHelper _output;

        public SmokeTests_OnIIS(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        //[InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable)]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone)]
        public async Task SmokeTestSuite_On_IIS_X86(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType)
        {
            var smokeTestRunner = new SmokeTests(_output);
            await smokeTestRunner.SmokeTestSuite(
                serverType, runtimeFlavor, architecture, applicationType, noSource: true);
        }
    }

    public class SmokeTests : LoggedTest
    {
        public SmokeTests(ITestOutputHelper output) : base(output)
        {
        }

        public async Task SmokeTestSuite(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource = false)
        {
            var testName = $"SmokeTestSuite_{serverType}_{runtimeFlavor}_{architecture}_{applicationType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger("SmokeTestSuite");
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    EnvironmentName = "SocialTesting",
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "MusicStoreTestSite",
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0",
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
