using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // Uses ports ranging 5001 - 5025.
    // TODO: temporarily disabling these tests as dotnet xunit runner does not support 32-bit yet.
    // public
    class SmokeTests_X86 : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_X86(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5002/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, "http://localhost:5003/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5004/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5005/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, "http://localhost:5006/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5007/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5008/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, "http://localhost:5009/")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl);
        }

        [ConditionalTheory(Skip = "Temporarily disabling test"), Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5010/")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    public class SmokeTests_X64 : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_X64(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5011/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5012/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5013/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5014/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5015/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5016/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5017/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5018/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5019/")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5020/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5021/")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl);
        }
        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    class SmokeTests_OnIIS : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_OnIIS(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [SkipIfEnvironmentVariableNotEnabled("IIS_VARIATIONS_ENABLED")]
        //[InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5022/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5023/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5024/")]
        public async Task SmokeTestSuite_On_IIS_X86(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(
                serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl, noSource: true);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    public class SmokeTests
    {
        private ILogger _logger;

        public SmokeTests(ILogger logger)
        {
            _logger = logger;
        }

        public async Task SmokeTestSuite(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource = false)
        {
            using (_logger.BeginScope("SmokeTestSuite"))
            {
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "SocialTesting",
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "MusicStoreTestSite",
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0",
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    ApplicationType = applicationType,
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, _logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, _logger))
                {
                    var deploymentResult = deployer.Deploy();

                    Helpers.SetInMemoryStoreForIIS(deploymentParameters, _logger);

                    await SmokeTestHelper.RunTestsAsync(deploymentResult, _logger);
                }
            }
        }
    }
}
