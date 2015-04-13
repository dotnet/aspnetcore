using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using DeploymentHelpers;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    // Uses ports ranging 5040 - 5049.
    public class OpenIdConnectTests
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5040/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5041/")]
        public void OpenIdConnect_OnX86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.mono, RuntimeArchitecture.x86, "http://localhost:5042/")]
        public void OpenIdConnect_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            OpenIdConnectTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        private void OpenIdConnectTestSuite(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("OpenId:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("OpenIdConnectTestSuite"))
            {
                var stopwatch = Stopwatch.StartNew();

                logger.LogInformation("Variation Details : HostType = {hostType}, DonetFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);
                logger.LogInformation("Pointing MusicStore DB to '{connString}'", connectionString);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "OpenIdConnectTesting",
                    UserAdditionalCleanup = parameters =>
                    {
                        if (!Helpers.RunningOnMono)
                        {
                            // Mono uses InMemoryStore
                            DbUtils.DropDatabase(musicStoreDbName, logger);
                        }
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>("SQLAZURECONNSTR_DefaultConnection", connectionString));

                bool testSuccessful = false;

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    HttpResponseMessage response = null;

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    RetryHelper.RetryRequest(() =>
                    {
                        response = httpClient.GetAsync(string.Empty).Result;
                        return response;
                    }, logger: logger);

                    logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);
                    validator.VerifyHomePage(response);

                    // OpenIdConnect login.
                    validator.LoginWithOpenIdConnect();

                    stopwatch.Stop();
                    logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                    testSuccessful = true;
                }

                if (!testSuccessful)
                {
                    logger.LogError("Some tests failed.");
                }
            }
        }
    }
}