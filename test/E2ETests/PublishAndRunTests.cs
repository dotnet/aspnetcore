using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class PublishAndRunTests_OnX64 : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public PublishAndRunTests_OnX64(ITestOutputHelper output)
        {
            _loggerFactory = new LoggerFactory()
                .AddXunit(output);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        //[InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, false,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        // [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, false,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_loggerFactory);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, noSource);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone, false,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_loggerFactory);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, noSource);
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }

    // TODO: temporarily disabling x86 tests as dotnet xunit test runner currently does not support 32-bit
    // public
    class PublishAndRunTests_OnX86 : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public PublishAndRunTests_OnX86(ITestOutputHelper output)
        {
            _loggerFactory = new LoggerFactory()
                .AddXunit(output);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, ApplicationType.Standalone, false)]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_loggerFactory);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, noSource);
        }

        [ConditionalTheory, Trait("E2Etests", "PublishAndRun")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, ApplicationType.Portable, false)]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testRunner = new PublishAndRunTests(_loggerFactory);
            await testRunner.Publish_And_Run_Tests(
                serverType, runtimeFlavor, architecture, applicationType, noSource);
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }

    public class PublishAndRunTests
    {
        private ILoggerFactory _loggerFactory;

        public PublishAndRunTests(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public async Task Publish_And_Run_Tests(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            ApplicationType applicationType,
            bool noSource)
        {
            var testName = $"PublishAndRunTests:{serverType}:{runtimeFlavor}:{architecture}:{applicationType}:NoSource={noSource}";
            try
            {
                Console.WriteLine($"Starting {testName}");
                var logger = _loggerFactory.CreateLogger(testName);
                using (logger.BeginScope("Publish_And_Run_Tests"))
                {
                    var musicStoreDbName = DbUtils.GetUniqueName();

                    var deploymentParameters = new DeploymentParameters(
                        Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                    {
                        PublishApplicationBeforeDeployment = true,
                        PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                        TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0",
                        Configuration = Helpers.GetCurrentBuildConfiguration(),
                        ApplicationType = applicationType,
                        UserAdditionalCleanup = parameters =>
                        {
                            DbUtils.DropDatabase(musicStoreDbName, logger);
                        }
                    };

                    if (applicationType == ApplicationType.Standalone)
                    {
                        deploymentParameters.AdditionalPublishParameters = "-r " + RuntimeEnvironment.GetRuntimeIdentifier();
                    }

                    // Override the connection strings using environment based configuration
                    deploymentParameters.EnvironmentVariables
                        .Add(new KeyValuePair<string, string>(
                            MusicStoreConfig.ConnectionStringKey,
                            DbUtils.CreateConnectionString(musicStoreDbName)));

                    using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, _loggerFactory))
                    {
                        var deploymentResult = await deployer.DeployAsync();
                        var httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                        var httpClient = new HttpClient(httpClientHandler);
                        httpClient.BaseAddress = new Uri(deploymentResult.ApplicationBaseUri);

                        // Request to base address and check if various parts of the body are rendered &
                        // measure the cold startup time.
                        // Add retry logic since tests are flaky on mono due to connection issues
                        var response = await RetryHelper.RetryRequest(async () => await httpClient.GetAsync(string.Empty), logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                        Assert.False(response == null, "Response object is null because the client could not " +
                            "connect to the server after multiple retries");

                        var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

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

                        logger.LogInformation("Variation completed successfully.");
                    }
                }
            }
            finally
            {
                Console.WriteLine($"Finished {testName}");
            }
        }
    }
}