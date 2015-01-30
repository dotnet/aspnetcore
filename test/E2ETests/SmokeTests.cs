using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private const string CONNECTION_STRING_FORMAT = "Server=(localdb)\\MSSQLLocalDB;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";

        private string _applicationBaseUrl;
        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;
        private StartParameters _startParameters;
        private readonly ILogger _logger;

        public SmokeTests()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            _logger = loggerFactory.Create<SmokeTests>();
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.DesktopClr, RuntimeArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5004/")]
        public void SmokeTestSuite_OnX86(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, runtimeFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [SkipOn32BitOS]
        [InlineData(ServerType.WebListener, RuntimeFlavor.DesktopClr, RuntimeArchitecture.amd64, "http://localhost:5002/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5001/")]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5004/")]
        public void SmokeTestSuite_OnAMD64(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, donetFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.DotNet)]
        [InlineData(ServerType.Kestrel, RuntimeFlavor.Mono, RuntimeArchitecture.x86, "http://localhost:5004/")]
        public void SmokeTestSuite_OnMono(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, donetFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfNativeModuleNotInstalled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5005/")]
        public void SmokeTestSuite_On_NativeModule_X86(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, donetFlavor, architecture, applicationBaseUrl);
        }

        [ConditionalTheory]
        [SkipIfNativeModuleNotInstalled]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Win7And2008R2 | OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [SkipOn32BitOS]
        [InlineData(ServerType.IISNativeModule, RuntimeFlavor.CoreClr, RuntimeArchitecture.amd64, "http://localhost:5005/")]
        public void SmokeTestSuite_On_NativeModule_AMD64(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, donetFlavor, architecture, applicationBaseUrl);
        }

        // [ConditionalTheory]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Unix)]
        [InlineData(ServerType.IIS, RuntimeFlavor.CoreClr, RuntimeArchitecture.x86, "http://localhost:5005/")]
        public void SmokeTestSuite_On_IIS_X86(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            SmokeTestSuite(serverType, donetFlavor, architecture, applicationBaseUrl);
        }

        private void SmokeTestSuite(ServerType serverType, RuntimeFlavor donetFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            using (_logger.BeginScope("SmokeTestSuite"))
            {
                _logger.WriteInformation("Variation Details : HostType = {0}, DonetFlavor = {1}, Architecture = {2}, applicationBaseUrl = {3}",
                    serverType, donetFlavor, architecture, applicationBaseUrl);

                _startParameters = new StartParameters
                {
                    ServerType = serverType,
                    RuntimeFlavor = donetFlavor,
                    RuntimeArchitecture = architecture,
                    EnvironmentName = "SocialTesting"
                };

                var testStartTime = DateTime.Now;
                var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

                _logger.WriteInformation("Pointing MusicStore DB to '{0}'", string.Format(CONNECTION_STRING_FORMAT, musicStoreDbName));

                //Override the connection strings using environment based configuration
                Environment.SetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection", string.Format(CONNECTION_STRING_FORMAT, musicStoreDbName));

                _applicationBaseUrl = applicationBaseUrl;
                Process hostProcess = null;
                bool testSuccessful = false;

                try
                {
                    hostProcess = DeploymentUtility.StartApplication(_startParameters, musicStoreDbName, _logger);
                    if (serverType == ServerType.IISNativeModule || serverType == ServerType.IIS)
                    {
                        // Accomodate the vdir name.
                        _applicationBaseUrl += _startParameters.IISApplication.VirtualDirectoryName + "/";
                    }

                    _httpClientHandler = new HttpClientHandler();
                    _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_applicationBaseUrl) };

                    HttpResponseMessage response = null;
                    string responseContent = null;
                    var initializationCompleteTime = DateTime.MinValue;

                    //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    Helpers.Retry(() =>
                    {
                        response = _httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        initializationCompleteTime = DateTime.Now;
                    }, logger: _logger);

                    _logger.WriteInformation("[Time]: Approximate time taken for application initialization : '{0}' seconds",
                                (initializationCompleteTime - testStartTime).TotalSeconds);

                    VerifyHomePage(response, responseContent);

                    //Verify the static file middleware can serve static content
                    VerifyStaticContentServed();

                    //Making a request to a protected resource should automatically redirect to login page
                    AccessStoreWithoutPermissions();

                    //Register a user - Negative scenario where the Password & ConfirmPassword do not match
                    RegisterUserWithNonMatchingPasswords();

                    //Register a valid user
                    var generatedEmail = RegisterValidUser();

                    SignInWithUser(generatedEmail, "Password~1");

                    //Register a user - Negative scenario : Trying to register a user name that's already registered.
                    RegisterExistingUser(generatedEmail);

                    //Logout from this user session - This should take back to the home page
                    SignOutUser(generatedEmail);

                    //Sign in scenarios: Invalid password - Expected an invalid user name password error.
                    SignInWithInvalidPassword(generatedEmail, "InvalidPassword~1");

                    //Sign in scenarios: Valid user name & password.
                    SignInWithUser(generatedEmail, "Password~1");

                    //Change password scenario
                    ChangePassword(generatedEmail);

                    //SignIn with old password and verify old password is not allowed and new password is allowed
                    SignOutUser(generatedEmail);
                    SignInWithInvalidPassword(generatedEmail, "Password~1");
                    SignInWithUser(generatedEmail, "Password~2");

                    //Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                    AccessStoreWithoutPermissions(generatedEmail);

                    //Logout from this user session - This should take back to the home page
                    SignOutUser(generatedEmail);

                    //Login as an admin user
                    SignInWithUser("Administrator@test.com", "YouShouldChangeThisPassword1!");

                    //Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                    AccessStoreWithPermissions();

                    //Create an album
                    var albumName = CreateAlbum();
                    var albumId = FetchAlbumIdFromName(albumName);

                    //Get details of the album
                    VerifyAlbumDetails(albumId, albumName);

                    //Get the non-admin view of the album.
                    GetAlbumDetailsFromStore(albumId, albumName);

                    //Add an album to cart and checkout the same
                    AddAlbumToCart(albumId, albumName);
                    CheckOutCartItems();

                    //Delete the album from store
                    DeleteAlbum(albumId, albumName);

                    //Logout from this user session - This should take back to the home page
                    SignOutUser("Administrator");

                    //Google login
                    LoginWithGoogle();

                    //Facebook login
                    LoginWithFacebook();

                    //Twitter login
                    LoginWithTwitter();

                    //MicrosoftAccountLogin
                    LoginWithMicrosoftAccount();

                    var testCompletionTime = DateTime.Now;
                    _logger.WriteInformation("[Time]: All tests completed in '{0}' seconds", (testCompletionTime - initializationCompleteTime).TotalSeconds);
                    _logger.WriteInformation("[Time]: Total time taken for this test variation '{0}' seconds", (testCompletionTime - testStartTime).TotalSeconds);
                    testSuccessful = true;
                }
                finally
                {
                    if (!testSuccessful)
                    {
                        _logger.WriteError("Some tests failed. Proceeding with cleanup.");
                    }

                    DeploymentUtility.CleanUpApplication(_startParameters, hostProcess, musicStoreDbName, _logger);
                }
            }
        }
    }
}