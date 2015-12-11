using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Testing;
using Microsoft.AspNet.Testing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace E2ETests
{
    // Uses ports ranging 5040 - 5049.
    public class OpenIdConnectTests
    {
        [ConditionalTheory(Skip = "Temporarily skipped the test to fix potential product issue"), Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5040/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5041/")]
        public async Task OpenIdConnect_OnX86(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory(Skip = "Bug https://github.com/aspnet/dnx/issues/2958"), Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5042/")]
        public async Task OpenIdConnect_OnNonWindows(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory(Skip = "https://github.com/aspnet/MusicStore/issues/565"), Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5043/")]
        public async Task OpenIdConnect_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            await OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        private async Task OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole(LogLevel.Warning)
                            .CreateLogger(string.Format("OpenId:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("OpenIdConnectTestSuite"))
            {
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "OpenIdConnectTesting",
                    UserAdditionalCleanup = parameters =>
                    {
                        if (!Helpers.RunningOnMono
                            && TestPlatformHelper.IsWindows)
                        {
                            // Mono uses InMemoryStore
                            DbUtils.DropDatabase(musicStoreDbName, logger);
                        }
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        "SQLAZURECONNSTR_DefaultConnection",
                        string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);
                    await validator.VerifyHomePage(response);

                    // OpenIdConnect login.
                    await validator.LoginWithOpenIdConnect();

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}