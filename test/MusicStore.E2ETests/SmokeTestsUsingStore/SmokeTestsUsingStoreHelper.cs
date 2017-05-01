using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class SmokeTestsUsingStoreHelper : LoggedTest
    {
        public SmokeTestsUsingStoreHelper(ITestOutputHelper output) : base(output)
        {
        }

        public async Task SmokeTestSuite(ServerType serverType, bool isStoreInDefaultLocation, string storeDirectory)
        {
            var targetFramework = "netcoreapp2.0";
            var testName = $"SmokeTestsUsingStore_{serverType}";
            using (StartLog(out var loggerFactory, testName))
            {
                var logger = loggerFactory.CreateLogger(nameof(SmokeTestsUsingStoreHelper));
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(ApplicationType.Portable), serverType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
                {
                    EnvironmentName = "SocialTesting",
                    SiteName = "MusicStoreTestSiteUsingStore",
                    PublishApplicationBeforeDeployment = true,
                    PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                    TargetFramework = targetFramework,
                    Configuration = Helpers.GetCurrentBuildConfiguration(),
                    ApplicationType = ApplicationType.Portable,
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

                if (!isStoreInDefaultLocation)
                {
                    deploymentParameters.EnvironmentVariables.Add(
                        new KeyValuePair<string, string>("DOTNET_SHARED_STORE", storeDirectory));
                }

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, loggerFactory))
                {
                    var deploymentResult = await deployer.DeployAsync();

                    var mvcCoreDllPath = Path.Combine(deploymentResult.ContentRoot, "Microsoft.AspNetCore.Mvc.Core.dll");
                    var fileInfo = new FileInfo(mvcCoreDllPath);
                    Assert.False(
                        File.Exists(mvcCoreDllPath),
                        $"The file '{fileInfo.Name}.{fileInfo.Extension}' was not expected to be present in the publish directory");

                    await SmokeTestHelper.RunTestsAsync(deploymentResult, logger);
                }
            }
        }
    }
}
