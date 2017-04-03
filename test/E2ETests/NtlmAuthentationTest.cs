using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    public class NtlmAuthenticationTests : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public NtlmAuthenticationTests(ITestOutputHelper output)
        {
            _loggerFactory = new LoggerFactory()
                .AddXunit(output);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Portable)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, ApplicationType.Standalone,
            Skip = "https://github.com/aspnet/MusicStore/issues/761")]
        public async Task NtlmAuthenticationTest(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, ApplicationType applicationType)
        {
            try
            {
                Console.WriteLine("NtlmAuthenticationTest");
                var logger = _loggerFactory.CreateLogger($"NtlmAuthentication:{serverType}:{runtimeFlavor}:{architecture}:{applicationType}");
                using (logger.BeginScope("NtlmAuthenticationTest"))
                {
                    var musicStoreDbName = DbUtils.GetUniqueName();

                    var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(applicationType), serverType, runtimeFlavor, architecture)
                    {
                        PublishApplicationBeforeDeployment = true,
                        PreservePublishedApplicationForDebugging = Helpers.PreservePublishedApplicationForDebugging,
                        TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0",
                        Configuration = Helpers.GetCurrentBuildConfiguration(),
                        ApplicationType = applicationType,
                        EnvironmentName = "NtlmAuthentication", //Will pick the Start class named 'StartupNtlmAuthentication'
                        ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NtlmAuthentation.config")) : null,
                        SiteName = "MusicStoreNtlmAuthentication", //This is configured in the NtlmAuthentication.config
                        UserAdditionalCleanup = parameters =>
                        {
                            DbUtils.DropDatabase(musicStoreDbName, logger);
                        }
                    };

                    if (applicationType == ApplicationType.Standalone)
                    {
                        deploymentParameters.AdditionalPublishParameters = " -r " + RuntimeEnvironment.GetRuntimeIdentifier();
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
                        var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                        // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                        var response = await RetryHelper.RetryRequest(async () =>
                        {
                            return await httpClient.GetAsync(string.Empty);
                        }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                        Assert.False(response == null, "Response object is null because the client could not " +
                            "connect to the server after multiple retries");

                        var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

                        Console.WriteLine("Verifying home page");
                        await validator.VerifyNtlmHomePage(response);

                        Console.WriteLine("Verifying access to store with permissions");
                        await validator.AccessStoreWithPermissions();

                        logger.LogInformation("Variation completed successfully.");
                    }
                }
            }
            finally
            {
                Console.WriteLine("Finished NtlmAuthenticationTest");
            }
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }
    }
}
