using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using E2ETests.Common;
using Microsoft.AspNetCore.Server.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace E2ETests
{
    // Uses ports ranging 5001 - 5025.
    // TODO: temporarily disabling these tests as dotnet xunit runner does not support 32-bit yet.
    // public
    class SmokeTests_X86 : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_X86(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5003/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5005/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5006/")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory(Skip = "Temporarily disabling test"), Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5007/")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    public class SmokeTests_X64 : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_X64(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5008/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5009/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5010/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5011/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5012/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5013/")]
        public async Task WindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5014/")]
        public async Task NonWindowsOS(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    class SmokeTests_OnIIS : IDisposable
    {
        private readonly XunitLogger _logger;

        public SmokeTests_OnIIS(ITestOutputHelper output)
        {
            _logger = new XunitLogger(output, LogLevel.Information);
        }

        [ConditionalTheory, Trait("E2Etests", "Smoke")]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [SkipIfIISVariationsNotEnabled]
        //[InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5015/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5016/")]
        public async Task SmokeTestSuite_On_IIS_X86(
            ServerType serverType,
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests(_logger);
            await smokeTestRunner.SmokeTestSuite(
                serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource: true);
        }

        public void Dispose()
        {
            _logger.Dispose();
        }
    }

    public class SmokeTests
    {
        private ILogger _logger;

        public SmokeTests(ILogger logger)
        {
            _logger = logger;
        }

        public async Task SmokeTestSuite(
            ServerType serverType,
            RuntimeFlavor donetFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl,
            bool noSource = false)
        {
            using (_logger.BeginScope("SmokeTestSuite"))
            {
                var musicStoreDbName = DbUtils.GetUniqueName();

                var deploymentParameters = new DeploymentParameters(
                    Helpers.GetApplicationPath(), serverType, donetFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "SocialTesting",
                    ServerConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("Http.config") : null,
                    SiteName = "MusicStoreTestSite",
                    PublishApplicationBeforeDeployment = true,
                    PublishTargetFramework = donetFlavor == RuntimeFlavor.Clr ? "net451" : "netstandardapp1.5",
                    UserAdditionalCleanup = parameters =>
                    {
                        DbUtils.DropDatabase(musicStoreDbName, _logger);
                    }
                };

                // Override the connection strings using environment based configuration
                deploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>(
                        MusicStore.StoreConfig.ConnectionStringKey,
                        DbUtils.CreateConnectionString(musicStoreDbName)));

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, _logger))
                {
                    var deploymentResult = deployer.Deploy();
                    Helpers.SetInMemoryStoreForIIS(deploymentParameters, _logger);

                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler)
                    {
                        BaseAddress = new Uri(deploymentResult.ApplicationBaseUri),
                        Timeout = TimeSpan.FromSeconds(5),
                    };

                    // Request to base address and check if various parts of the body are rendered
                    // & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: _logger, cancellationToken: deploymentResult.HostShutdownToken);

                    Assert.False(response == null, "Response object is null because the client could not " +
                        "connect to the server after multiple retries");

                    var validator = new Validator(httpClient, httpClientHandler, _logger, deploymentResult);

                    await validator.VerifyHomePage(response);

                    // Verify the static file middleware can serve static content.
                    await validator.VerifyStaticContentServed();

                    // Making a request to a protected resource should automatically redirect to login page.
                    await validator.AccessStoreWithoutPermissions();

                    // Register a user - Negative scenario where the Password & ConfirmPassword do not match.
                    await validator.RegisterUserWithNonMatchingPasswords();

                    // Register a valid user.
                    var generatedEmail = await validator.RegisterValidUser();

                    await validator.SignInWithUser(generatedEmail, "Password~1");

                    // Register a user - Negative scenario : Trying to register a user name that's already registered.
                    await validator.RegisterExistingUser(generatedEmail);

                    // Logout from this user session - This should take back to the home page
                    await validator.SignOutUser(generatedEmail);

                    // Sign in scenarios: Invalid password - Expected an invalid user name password error.
                    await validator.SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

                    // Sign in scenarios: Valid user name & password.
                    await validator.SignInWithUser(generatedEmail, "Password~1");

                    // Change password scenario
                    await validator.ChangePassword(generatedEmail);

                    // SignIn with old password and verify old password is not allowed and new password is allowed
                    await validator.SignOutUser(generatedEmail);
                    await validator.SignInWithInvalidPassword(generatedEmail, "Password~1");
                    await validator.SignInWithUser(generatedEmail, "Password~2");

                    // Making a request to a protected resource that this user does not have access to - should
                    // automatically redirect to the configured access denied page
                    await validator.AccessStoreWithoutPermissions(generatedEmail);

                    // Logout from this user session - This should take back to the home page
                    await validator.SignOutUser(generatedEmail);

                    // Login as an admin user
                    await validator.SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

                    // Now navigating to the store manager should work fine as this user has
                    // the necessary permission to administer the store.
                    await validator.AccessStoreWithPermissions();

                    // Create an album
                    var albumName = await validator.CreateAlbum();
                    var albumId = await validator.FetchAlbumIdFromName(albumName);

                    // Get details of the album
                    await validator.VerifyAlbumDetails(albumId, albumName);

                    // Verify status code pages acts on non-existing items.
                    await validator.VerifyStatusCodePages();

                    // Get the non-admin view of the album.
                    await validator.GetAlbumDetailsFromStore(albumId, albumName);

                    // Add an album to cart and checkout the same
                    await validator.AddAlbumToCart(albumId, albumName);
                    await validator.CheckOutCartItems();

                    // Delete the album from store
                    await validator.DeleteAlbum(albumId, albumName);

                    // Logout from this user session - This should take back to the home page
                    await validator.SignOutUser("Administrator");

                    // Google login
                    await validator.LoginWithGoogle();

                    // Facebook login
                    await validator.LoginWithFacebook();

                    // Twitter login
                    await validator.LoginWithTwitter();

                    // MicrosoftAccountLogin
                    await validator.LoginWithMicrosoftAccount();

                    _logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
