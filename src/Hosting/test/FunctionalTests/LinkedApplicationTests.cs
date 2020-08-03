using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class LinkedApplicationTests : LoggedTest
    {
        [Fact]
        public async Task LinkedApplicationWorks()
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger("LinkedApplicationWorks");

                // https://github.com/dotnet/aspnetcore/issues/8247
#pragma warning disable 0618
                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Hosting"), "test", "testassets",
                    "BasicLinkedApp");
#pragma warning restore 0618

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    ServerType.Kestrel,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    TargetFramework = Tfm.Net50,
                    RuntimeArchitecture = RuntimeArchitecture.x64,
                    ApplicationType = ApplicationType.Standalone,
                    PublishApplicationBeforeDeployment = true,
                    RestoreDependencies = true,
                    StatusMessagesEnabled = false
                };

                using var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory);

                var result = await deployer.DeployAsync();

                // The app should have started up
                Assert.False(deployer.HostProcess.HasExited);

                var response = await RetryHelper.RetryRequest(() => result.HttpClient.GetAsync("/"), logger, retryCount: 10);
                var body = await response.Content.ReadAsStringAsync();

                Assert.Equal("Hello World", body);
            }
        }
    }
}
