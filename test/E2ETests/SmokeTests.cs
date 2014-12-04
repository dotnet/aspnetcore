using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private const string Connection_string_Format = "Server=(localdb)\\MSSQLLocalDB;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";

        private string ApplicationBaseUrl;
        private HttpClient httpClient;
        private HttpClientHandler httpClientHandler;

        [Theory]
        [InlineData(ServerType.Helios, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5002/", false)]
        [InlineData(ServerType.Kestrel, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5004/", false)]
        [InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        [InlineData(ServerType.WebListener, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5002/", false)]
        [InlineData(ServerType.Kestrel, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5004/", false)]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.amd64, "http://localhost:5002/", false)]
        [InlineData(ServerType.Kestrel, KreFlavor.Mono, KreArchitecture.x86, "http://localhost:5004/", true)]
        [InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.amd64, "http://localhost:5001/", false)]
        [InlineData(ServerType.Kestrel, KreFlavor.CoreClr, KreArchitecture.amd64, "http://localhost:5004/", false)]
        //Native module variation requires some more work
        //[InlineData(ServerType.HeliosNativeModule, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5001/", false)]
        public void SmokeTestSuite(ServerType serverType, KreFlavor kreFlavor, KreArchitecture architecture, string applicationBaseUrl, bool RunTestOnMono)
        {
            Console.WriteLine("Variation Details : HostType = {0}, KreFlavor = {1}, Architecture = {2}, applicationBaseUrl = {3}", serverType, kreFlavor, architecture, applicationBaseUrl);

            if (Helpers.SkipTestOnCurrentConfiguration(RunTestOnMono, architecture))
            {
                Assert.True(true);
                return;
            }

            var startParameters = new StartParameters
            {
                ServerType = serverType,
                KreFlavor = kreFlavor,
                KreArchitecture = architecture,
                ApplicationHostConfigTemplateContent = (serverType == ServerType.HeliosNativeModule) ? File.ReadAllText("HeliosNativeModuleApplicationHost.config") : null,
                SiteName = (serverType == ServerType.HeliosNativeModule) ? "MusicStoreNativeModule" : null,
                EnvironmentName = "SocialTesting"
            };

            var testStartTime = DateTime.Now;
            var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            Console.WriteLine("Pointing MusicStore DB to '{0}'", string.Format(Connection_string_Format, musicStoreDbName));

            //Override the connection strings using environment based configuration
            Environment.SetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection", string.Format(Connection_string_Format, musicStoreDbName));

            ApplicationBaseUrl = applicationBaseUrl;
            Process hostProcess = null;
            bool testSuccessful = false;

            try
            {
                hostProcess = DeploymentUtility.StartApplication(startParameters, musicStoreDbName);

                httpClientHandler = new HttpClientHandler();
                httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                HttpResponseMessage response = null;
                string responseContent = null;
                var initializationCompleteTime = DateTime.MinValue;

                //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                for (int retryCount = 0; retryCount < 3; retryCount++)
                {
                    try
                    {
                        response = httpClient.GetAsync(string.Empty).Result;
                        responseContent = response.Content.ReadAsStringAsync().Result;
                        initializationCompleteTime = DateTime.Now;
                        Console.WriteLine("[Time]: Approximate time taken for application initialization : '{0}' seconds", (initializationCompleteTime - testStartTime).TotalSeconds);
                        break; //Went through successfully
                    }
                    catch (AggregateException exception)
                    {
                        if (exception.InnerException is HttpRequestException || exception.InnerException is WebException)
                        {
                            Console.WriteLine("Failed to complete the request with error: {0}", exception.ToString());
                            Console.WriteLine("Retrying request..");
                            Thread.Sleep(1 * 1000); //Wait for a second before retry
                        }
                    }
                }

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
                Console.WriteLine("[Time]: All tests completed in '{0}' seconds", (testCompletionTime - initializationCompleteTime).TotalSeconds);
                Console.WriteLine("[Time]: Total time taken for this test variation '{0}' seconds", (testCompletionTime - testStartTime).TotalSeconds);
                testSuccessful = true;
            }
            finally
            {
                if (!testSuccessful)
                {
                    Console.WriteLine("Some tests failed. Proceeding with cleanup.");
                }

                DeploymentUtility.CleanUpApplication(startParameters, hostProcess, musicStoreDbName);
            }
        }
    }
}