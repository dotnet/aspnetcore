using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class SmokeTestsOnNanoServer : IDisposable
    {
        private readonly XunitLogger _logger;
        private readonly RemoteDeploymentConfig _remoteDeploymentConfig;

        public SmokeTestsOnNanoServer(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("remoteDeploymentConfig.json")
                .AddEnvironmentVariables()
                .Build();

            _remoteDeploymentConfig = new RemoteDeploymentConfig();
            configuration.GetSection("NanoServer").Bind(_remoteDeploymentConfig);
        }

        [ConditionalTheory, Trait("E2Etests", "NanoServer")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [SkipIfEnvironmentVariableNotEnabled("RUN_TESTS_ON_NANO")]
        [InlineData(ServerType.Kestrel, 5000, ApplicationType.Portable)]
        [InlineData(ServerType.Kestrel, 5000, ApplicationType.Standalone)]
        [InlineData(ServerType.WebListener, 5000, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, 5000, ApplicationType.Standalone)]
        public async Task Test(ServerType serverType, int portToListen, ApplicationType applicationType)
        {
            var applicationBaseUrl = $"http://{_remoteDeploymentConfig.ServerName}:{portToListen}/";
            await RunTestsAsync(serverType, applicationBaseUrl, applicationType);
        }

        private async Task RunTestsAsync(ServerType serverType, string applicationBaseUrl, ApplicationType applicationType)
        {
            using (_logger.BeginScope("SmokeTestSuite"))
            {
                var deploymentParameters = new RemoteWindowsDeploymentParameters(
                    Helpers.GetApplicationPath(applicationType),
                    serverType,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64,
                    _remoteDeploymentConfig.FileSharePath,
                    _remoteDeploymentConfig.ServerName,
                    _remoteDeploymentConfig.AccountName,
                    _remoteDeploymentConfig.AccountPassword,
                    _remoteDeploymentConfig.ExecutableRelativePath)
                {
                    TargetFramework = "netcoreapp1.0",
                    ApplicationBaseUriHint = applicationBaseUrl
                };
                deploymentParameters.EnvironmentVariables.Add(
                    new KeyValuePair<string, string>("ASPNETCORE_ENVIRONMENT", "SocialTesting"));

                using (var deployer = new RemoteWindowsDeployer(deploymentParameters, _logger))
                {
                    var deploymentResult = deployer.Deploy();

                    await SmokeTestHelper.RunTestsAsync(deploymentResult, _logger);
                }
            }
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }
}
