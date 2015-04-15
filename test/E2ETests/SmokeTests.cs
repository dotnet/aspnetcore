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
    // Uses ports ranging 5001 - 5025.
    public class SmokeTests_X86_Clr
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5003/")]
        public void SmokeTestSuite_OnX86_clr(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_X86_Coreclr
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5005/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5006/")]
        public void SmokeTestSuite_OnX86_coreclr(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_X64
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [SkipOn32BitOS]
        [InlineData(ServerType.WebListener, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5007/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5008/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5009/")]
        public void SmokeTestSuite_OnAMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnMono
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.mono, RuntimeArchitecture.x86, "http://localhost:5010/")]
        public void SmokeTestSuite_OnMono(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnIIS_NativeModule
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [SkipIfIISNativeVariationsNotEnabled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5011/")]
        public void SmokeTestSuite_On_NativeModule_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [SkipIfIISNativeVariationsNotEnabled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipOn32BitOS]
        [SkipIfCurrentRuntimeIsCoreClr]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5012/")]
        public void SmokeTestSuite_On_NativeModule_AMD64(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }
    }

    public class SmokeTests_OnIIS
    {
        [ConditionalTheory, Trait("E2Etests", "E2Etests")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipIfCurrentRuntimeIsCoreClr]
        [SkipIfIISVariationsNotEnabled]
        [InlineData(ServerType.IIS, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5013/")]
        [InlineData(ServerType.IIS, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5013/")]
        public void SmokeTestSuite_On_IIS_X86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var smokeTestRunner = new SmokeTests();
            smokeTestRunner.SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl, noSource: true);
        }
    }

    public class SmokeTests
    {
        public void SmokeTestSuite(
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
                var stopwatch = Stopwatch.StartNew();

                logger.LogInformation("Variation Details : HostType = {hostType}, DonetFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, donetFlavor, architecture, applicationBaseUrl);

                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var connectionString = string.Format(DbUtils.CONNECTION_STRING_FORMAT, musicStoreDbName);
                logger.LogInformation("Pointing MusicStore DB to '{connString}'", connectionString);

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
                    .Add(new KeyValuePair<string, string>("SQLAZURECONNSTR_DefaultConnection", connectionString));

                bool testSuccessful = false;

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    Helpers.SetInMemoryStoreForIIS(deploymentParameters, logger);

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

                    // Verify the static file middleware can serve static content.
                    validator.VerifyStaticContentServed();

                    // Making a request to a protected resource should automatically redirect to login page.
                    validator.AccessStoreWithoutPermissions();

                    // Register a user - Negative scenario where the Password & ConfirmPassword do not match.
                    validator.RegisterUserWithNonMatchingPasswords();

                    // Register a valid user.
                    var generatedEmail = validator.RegisterValidUser();

                    validator.SignInWithUser(generatedEmail, "Password~1");

                    // Register a user - Negative scenario : Trying to register a user name that's already registered.
                    validator.RegisterExistingUser(generatedEmail);

                    // Logout from this user session - This should take back to the home page
                    validator.SignOutUser(generatedEmail);

                    // Sign in scenarios: Invalid password - Expected an invalid user name password error.
                    validator.SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

                    // Sign in scenarios: Valid user name & password.
                    validator.SignInWithUser(generatedEmail, "Password~1");

                    // Change password scenario
                    validator.ChangePassword(generatedEmail);

                    // SignIn with old password and verify old password is not allowed and new password is allowed
                    validator.SignOutUser(generatedEmail);
                    validator.SignInWithInvalidPassword(generatedEmail, "Password~1");
                    validator.SignInWithUser(generatedEmail, "Password~2");

                    // Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                    validator.AccessStoreWithoutPermissions(generatedEmail);

                    // Logout from this user session - This should take back to the home page
                    validator.SignOutUser(generatedEmail);

                    // Login as an admin user
                    validator.SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

                    // Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                    validator.AccessStoreWithPermissions();

                    // Create an album
                    var albumName = validator.CreateAlbum();
                    var albumId = validator.FetchAlbumIdFromName(albumName);

                    // Get details of the album
                    validator.VerifyAlbumDetails(albumId, albumName);

                    // Verify status code pages acts on non-existing items.
                    validator.VerifyStatusCodePages();

                    // Get the non-admin view of the album.
                    validator.GetAlbumDetailsFromStore(albumId, albumName);

                    // Add an album to cart and checkout the same
                    validator.AddAlbumToCart(albumId, albumName);
                    validator.CheckOutCartItems();

                    // Delete the album from store
                    validator.DeleteAlbum(albumId, albumName);

                    // Logout from this user session - This should take back to the home page
                    validator.SignOutUser("Administrator");

                    // Google login
                    validator.LoginWithGoogle();

                    // Facebook login
                    validator.LoginWithFacebook();

                    // Twitter login
                    validator.LoginWithTwitter();

                    // MicrosoftAccountLogin
                    validator.LoginWithMicrosoftAccount();

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