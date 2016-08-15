using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // Uses ports ranging 5025 - 5039.
    public class PublishAndRunTests_OnX64 : IDisposable
    {
        private readonly XunitLogger _logger;

        public PublishAndRunTests_OnX64(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5025/", false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5026/", false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5027/", false)]
        //[InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5028/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5029/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5030/", false)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_logger);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl, noSource);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, "http://localhost:5031/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, "http://localhost:5032/", false)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_logger);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl, noSource);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    // TODO: temporarily disabling x86 tests as dotnet xunit test runner currently does not support 32-bit
    // public
    class PublishAndRunTests_OnX86 : IDisposable
    {
        private readonly XunitLogger _logger;

        public PublishAndRunTests_OnX86(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5034/", false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5035/", false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, "http://localhost:5036/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5037/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5038/", false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, "http://localhost:5039/", false)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_logger);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl, noSource);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, "http://localhost:5040/", false)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_logger);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, applicationBaseUrl, noSource);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    public class PublishAndRunTests
    {
        private ILogger _logger;

        public PublishAndRunTests(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Publish_And_Run_Tests(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            string applicationBaseUrl,
            bool noSource)
        {
            using (_logger.BeginScope("Publish_And_Run_Tests"))
            {
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
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
                    var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                    var httpClient = new HttpClient(httpClientHandler);
                    httpClient.BaseAddress = new Uri(deploymentResult.ApplicationBaseUri);

                    // Request to base address and check if various parts of the body are rendered &
                    // measure the cold startup time.
                    // Add retry logic since tests are flaky on mono due to connection issues
                    var response = await RetryHelper.RetryRequest(async () => await httpClient.GetAsync(string.Empty), logger: _logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, _logger, deploymentResult);

                    Console.WriteLine("Verifying home page");
                    await validator.VerifyHomePage(response);

                    Console.WriteLine("Verifying static files are served from static file middleware");
                    await validator.VerifyStaticContentServed();

                    if (serverType != ServerType.IISExpress)
                    {
                        if (Directory.GetFiles(
                            deploymentParameters.ApplicationPath, "*.cmd", SearchOption.TopDirectoryOnly).Length > 0)
                        {
                            throw new Exception("publishExclude parameter values are not honored.");
                        }
                    }

                    _logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}