using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using DeploymentHelpers;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    // Uses ports ranging 5050 - 5060.
    public class NtlmAuthenticationTests
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Unix | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5050/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5051/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5052/")]
        public void NtlmAuthenticationTest(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("Ntlm:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("NtlmAuthenticationTest"))
            {
                var stopwatch = Stopwatch.StartNew();

                logger.LogInformation("Variation Details : HostType = {hostType}, RuntimeFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);
                logger.LogInformation("Pointing MusicStore DB to '{connString}'", connectionString);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                    ApplicationHostConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("NtlmAuthentation.config") : null,
                    SiteName = "MusicStoreNtlmAuthentication", //This is configured in the NtlmAuthentication.config
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
                    var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
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
                    validator.VerifyNtlmHomePage(response);

                    //Should be able to access the store as the Startup adds necessary permissions for the current user
                    validator.AccessStoreWithPermissions();

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