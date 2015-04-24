using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Testing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    // Uses ports ranging 5050 - 5060.
    public class NtlmAuthenticationTests
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5050/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5051/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5052/")]
        public async Task NtlmAuthenticationTest(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("Ntlm:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("NtlmAuthenticationTest"))
            {
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);

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
                    .Add(new KeyValuePair<string, string>(
                        "SQLAZURECONNSTR_DefaultConnection",
                        string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);
                    await validator.VerifyNtlmHomePage(response);

                    //Should be able to access the store as the Startup adds necessary permissions for the current user
                    await validator.AccessStoreWithPermissions();

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
