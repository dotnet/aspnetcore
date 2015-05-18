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
    // Uses ports ranging 5025 - 5039.
    public class PublishAndRunTests_OnX64
    {
        // https://github.com/aspnet/MusicStore/issues/487
        // [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5025/", false)]
        //https://github.com/aspnet/KRuntime/issues/642
        //[InlineData(ServerType.Helios, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5026/")]
        public async Task Publish_And_Run_Tests_On_AMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            var testRunner = new PublishAndRunTests();
            await testRunner.Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }
    }

    public class PublishAndRunTests_OnX86
    {
        // https://github.com/aspnet/MusicStore/issues/487
        // [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        //[InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5027/", false)]
        //[InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5028/", true)]
        public async Task Publish_And_Run_Tests_On_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            var testRunner = new PublishAndRunTests();
            await testRunner.Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }

        // https://github.com/aspnet/MusicStore/issues/487
        // [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR | RuntimeFrameworks.CoreCLR)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5029/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5030/", true)]
        public async Task Publish_And_Run_Tests_On_Mono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            var testRunner = new PublishAndRunTests();
            await testRunner.Publish_And_Run_Tests(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource);
        }
    }

    public class PublishAndRunTests
    {
        public async Task Publish_And_Run_Tests(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl, bool noSource)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("Publish:{0}:{1}:{2}:{3}", serverType, runtimeFlavor, architecture, noSource));

            using (logger.BeginScope("Publish_And_Run_Tests"))
            {
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    PublishApplicationBeforeDeployment = true,
                    PublishWithNoSource = noSource,
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
                    // Add retry logic since tests are flaky on mono due to connection issues
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);
                    await validator.VerifyHomePage(response);

                    // Static files are served?
                    await validator.VerifyStaticContentServed();

                    if (serverType != ServerType.IISExpress)
                    {
                        if (Directory.GetFiles(deploymentParameters.ApplicationPath, "*.cmd", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            throw new Exception("publishExclude parameter values are not honored.");
                        }
                    }

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}