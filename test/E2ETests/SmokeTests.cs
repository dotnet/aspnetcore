using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Testing;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    // Uses ports ranging 5001 - 5025.
    public class SmokeTests_X86_Clr
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5002/")]
        //https://github.com/aspnet/MusicStore/issues/488
        // [InlineData(ServerType.Kestrel, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5003/")]
        public async Task SmokeTestSuite_OnX86_clr(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_X86_Coreclr
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5005/")]
        // [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5006/")]
        public async Task SmokeTestSuite_OnX86_coreclr(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_X64
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [SkipOn32BitOS]
        [InlineData(ServerType.WebListener, RuntimeFlavor.Clr, RuntimeArchitecture.x64, "http://localhost:5007/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5008/")]
        // [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5009/")]
        public async Task SmokeTestSuite_OnAMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnMono
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR | RuntimeFrameworks.CoreCLR)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5010/")]
        public async Task SmokeTestSuite_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnIIS_NativeModule
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [SkipIfIISNativeVariationsNotEnabled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Linux)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5011/")]
        public async Task SmokeTestSuite_On_NativeModule_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [SkipIfIISNativeVariationsNotEnabled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Linux)]
        [SkipOn32BitOS]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5012/")]
        public async Task SmokeTestSuite_On_NativeModule_AMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnIIS
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [SkipIfIISVariationsNotEnabled]
        [InlineData(ServerType.IIS, RuntimeFlavor.Clr, RuntimeArchitecture.x86, "http://localhost:5013/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64, "http://localhost:5013/")]
        public async Task SmokeTestSuite_On_IIS_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            await smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource: true);
        }
    }

    public class SmokeTests
    {
        public async Task SmokeTestSuite(
            ServerType serverType,
            RuntimeFlavor donetFlavor,
            RuntimeArchitecture architecture,
            string applicationBaseUrl,
            bool noSource = false)
        {
            var logger = new LoggerFactory()
                           .AddConsole()
                           .CreateLogger(string.Format("Smoke:{0}:{1}:{2}", serverType, donetFlavor, architecture));

            using (logger.BeginScope("SmokeTestSuite"))
            {
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, donetFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "SocialTesting",
                    PublishWithNoSource = noSource,
                    UserAdditionalCleanup = parameters =>
                    {
                        if (!Helpers.RunningOnMono
                            && parameters.ServerType != ServerType.IIS
                            && parameters.ServerType != ServerType.IISNativeModule)
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
                    Helpers.SetInMemoryStoreForIIS(deploymentParameters, logger);

                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    var response = await RetryHelper.RetryRequest(async () =>
                    {
                        return await httpClient.GetAsync(string.Empty);
                    }, logger: logger, cancellationToken: deploymentResult.HostShutdownToken);

                    var validator = new Validator(httpClient, httpClientHandler, logger, deploymentResult);

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

                    // Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
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

                    logger.LogInformation("Variation completed successfully.");
                }
            }
        }
    }
}
