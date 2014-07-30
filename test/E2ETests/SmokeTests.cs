using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace E2ETests
{
    public class SmokeTests
    {
        private const string Connection_string_Format = "Server=(localdb)\\MSSQLLocalDB;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";

        private string ApplicationBaseUrl;
        private HttpClient httpClient;
        private HttpClientHandler httpClientHandler;

        [Theory]
        [InlineData(ServerType.Helios, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, KreFlavor.DesktopClr, KreArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5001/")]
        [InlineData(ServerType.WebListener, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5002/")]
        [InlineData(ServerType.Kestrel, KreFlavor.CoreClr, KreArchitecture.x86, "http://localhost:5004/")]
        [InlineData(ServerType.WebListener, KreFlavor.DesktopClr, KreArchitecture.x64, "http://localhost:5002/")]
        // Uncomment Core CLR on x64 after following bugs are resolved
        // https://github.com/aspnet/Identity/issues/157
        // https://github.com/aspnet/Mvc/issues/846
        //[InlineData(ServerType.Helios, KreFlavor.CoreClr, KreArchitecture.x64, "http://localhost:5001/")]
        //[InlineData(ServerType.Kestrel, KreFlavor.CoreClr, KreArchitecture.x64, "http://localhost:5004/")]
        public void SmokeTestSuite(ServerType hostType, KreFlavor kreFlavor, KreArchitecture architecture, string applicationBaseUrl)
        {
            Console.WriteLine("Variation Details : HostType = {0}, KreFlavor = {1}, Architecture = {2}, applicationBaseUrl = {3}", hostType, kreFlavor, architecture, applicationBaseUrl);

            // Check if processor architecture is x64, else skip test
            if (architecture == KreArchitecture.x64 && !Environment.Is64BitOperatingSystem)
            {
                Console.WriteLine("Skipping x64 test since machine is of type x86");
                Assert.True(true);
                return;
            }

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
                hostProcess = DeploymentUtility.StartApplication(hostType, kreFlavor, architecture, musicStoreDbName);
                httpClientHandler = new HttpClientHandler();
                httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                //Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                var response = httpClient.GetAsync(string.Empty).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                var initializationCompleteTime = DateTime.Now;
                Console.WriteLine("[Time]: Approximate time taken for application initialization : '{0}' seconds", (initializationCompleteTime - testStartTime).TotalSeconds);
                VerifyHomePage(response, responseContent);

                //Verify the static file middleware can serve static content
                VerifyStaticContentServed();

                //Making a request to a protected resource should automatically redirect to login page
                AccessStoreWithoutPermissions();

                //Register a user - Negative scenario where the Password & ConfirmPassword do not match
                RegisterUserWithNonMatchingPasswords();

                //Register a valid user
                var generatedUserName = RegisterValidUser();

                //Register a user - Negative scenario : Trying to register a user name that's already registered.
                RegisterExistingUser(generatedUserName);

                //Logout from this user session - This should take back to the home page
                SignOutUser(generatedUserName);

                //Sign in scenarios: Invalid password - Expected an invalid user name password error.
                SignInWithInvalidPassword(generatedUserName, "InvalidPassword~1");

                //Sign in scenarios: Valid user name & password.
                SignInWithUser(generatedUserName, "Password~1");

                //Change password scenario
                ChangePassword(generatedUserName);

                //SignIn with old password and verify old password is not allowed and new password is allowed
                SignOutUser(generatedUserName);
                SignInWithInvalidPassword(generatedUserName, "Password~1");
                SignInWithUser(generatedUserName, "Password~2");

                //Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                AccessStoreWithoutPermissions(generatedUserName);

                //Logout from this user session - This should take back to the home page
                SignOutUser(generatedUserName);

                //Login as an admin user
                SignInWithUser("Administrator", "YouShouldChangeThisPassword1!");

                //Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                AccessStoreWithPermissions();

                //Create an album
                var albumName = CreateAlbum();
                var albumId = FetchAlbumIdFromName(albumName);

                //Get details of the album
                VerifyAlbumDetails(albumId, albumName);

                //Add an album to cart and checkout the same
                AddAlbumToCart(albumId, albumName);
                CheckOutCartItems();

                //Delete the album from store
                DeleteAlbum(albumId, albumName);

                //Logout from this user session - This should take back to the home page
                SignOutUser("Administrator");

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

                if (hostProcess != null && !hostProcess.HasExited)
                {
                    //Shutdown the host process
                    hostProcess.Kill();
                    hostProcess.WaitForExit(5 * 1000);
                    if (!hostProcess.HasExited)
                    {
                        Console.WriteLine("Unable to terminate the host process with process Id '{0}", hostProcess.Id);
                    }
                    else
                    {
                        Console.WriteLine("Successfully terminated host process with process Id '{0}'", hostProcess.Id);
                    }
                }
                else
                {
                    Console.WriteLine("Host process already exited or never started successfully.");
                }

                DbUtils.DropDatabase(musicStoreDbName);
            }
        }

        private void VerifyStaticContentServed()
        {
            Console.WriteLine("Validating if static contents are served..");
            Console.WriteLine("Fetching favicon.ico..");
            var response = httpClient.GetAsync("/favicon.ico").Result;
            ThrowIfResponseStatusNotOk(response);

            Console.WriteLine("Fetching /Content/bootstrap.css..");
            response = httpClient.GetAsync("/Content/bootstrap.css").Result;
            ThrowIfResponseStatusNotOk(response);
            Console.WriteLine("Verified static contents are served successfully");
        }

        private void VerifyHomePage(HttpResponseMessage response, string responseContent)
        {
            Console.WriteLine("Home page content : {0}", responseContent);
            Assert.Equal<HttpStatusCode>(HttpStatusCode.OK, response.StatusCode);
            ValidateLayoutPage(responseContent);
            Assert.Contains("<a href=\"/Store/Details/", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<title>Home Page – MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Application initialization successful.");
        }

        private void ValidateLayoutPage(string responseContent)
        {
            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li><a href=\"/\">Home</a></li>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<a href=\"/Store\" class=\"dropdown-toggle\" data-toggle=\"dropdown\">Store <b class=\"caret\"></b></a>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<ul class=\"dropdown-menu\">", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li class=\"divider\"></li>", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        private void AccessStoreWithoutPermissions(string userName = null)
        {
            Console.WriteLine("Trying to access StoreManager that needs ManageStore claim with the current user : {0}", userName ?? "Anonymous");
            var response = httpClient.GetAsync("/StoreManager/").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);
            Assert.Contains("<title>Log in – MVC Music Store</title>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal<string>(ApplicationBaseUrl + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            Console.WriteLine("Redirected to login page as expected.");
        }

        private void AccessStoreWithPermissions()
        {
            Console.WriteLine("Trying to access the store inventory..");
            var response = httpClient.GetAsync("/StoreManager/").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(ApplicationBaseUrl + "StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);
            Console.WriteLine("Successfully acccessed the store inventory");
        }

        private void RegisterUserWithNonMatchingPasswords()
        {
            Console.WriteLine("Trying to create user with not matching password and confirm password");
            var response = httpClient.GetAsync("/Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);

            var generatedUserName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            Console.WriteLine("Creating a new user with name '{0}'", generatedUserName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", generatedUserName),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~2"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Contains("<div class=\"validation-summary-errors\" data-valmsg-summary=\"true\"><ul><li>The password and confirmation password do not match.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Server side model validator rejected the user '{0}''s registration as passwords do not match.", generatedUserName);
        }

        private string RegisterValidUser()
        {
            var response = httpClient.GetAsync("/Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);

            var generatedUserName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            Console.WriteLine("Creating a new user with name '{0}'", generatedUserName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", generatedUserName),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("Hello {0}!", generatedUserName), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Console.WriteLine("Successfully registered user '{0}' and signed in", generatedUserName);
            return generatedUserName;
        }

        private void RegisterExistingUser(string userName)
        {
            Console.WriteLine("Trying to register a user with name '{0}' again", userName);
            var response = httpClient.GetAsync("/Account/Register").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Creating a new user with name '{0}'", userName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", userName),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Register")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Register", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("Name {0} is already taken.", userName), responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Identity threw a valid exception that user '{0}' already exists in the system", userName);
        }

        private void SignOutUser(string userName)
        {
            Console.WriteLine("Signing out from '{0}''s session", userName);
            var response = httpClient.GetAsync(string.Empty).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            ValidateLayoutPage(responseContent);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/LogOff", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie cleared on logout
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Console.WriteLine("Successfully signed out of '{0}''s session", userName);
        }

        private void SignInWithInvalidPassword(string userName, string invalidPassword)
        {
            var response = httpClient.GetAsync("/Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with user '{0}'", userName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", userName),
                new KeyValuePair<string, string>("Password", invalidPassword),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<div class=\"validation-summary-errors\"><ul><li>Invalid username or password.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie not sent
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Console.WriteLine("Identity successfully prevented an invalid user login.");
        }

        private void SignInWithUser(string userName, string password)
        {
            var response = httpClient.GetAsync("/Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with user '{0}'", userName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", userName),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("Hello {0}!", userName), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Console.WriteLine("Successfully signed in with user '{0}'", userName);
        }

        private void ChangePassword(string userName)
        {
            var response = httpClient.GetAsync("/Account/Manage").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("OldPassword", "Password~1"),
                    new KeyValuePair<string, string>("NewPassword", "Password~2"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~2"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Manage")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Manage", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("Your password has been changed.", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Console.WriteLine("Successfully changed the password for user '{0}'", userName);
        }

        private string CreateAlbum()
        {
            var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
            Console.WriteLine("Trying to create an album with name '{0}'", albumName);
            var response = httpClient.GetAsync("/StoreManager/create").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/StoreManager/create")),
                    new KeyValuePair<string, string>("GenreId", "1"),
                    new KeyValuePair<string, string>("ArtistId", "1"),
                    new KeyValuePair<string, string>("Title", albumName),
                    new KeyValuePair<string, string>("Price", "9.99"),
                    new KeyValuePair<string, string>("AlbumArtUrl", "http://myapp/testurl"),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/StoreManager/create", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(ApplicationBaseUrl + "StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains(albumName, responseContent);
            Console.WriteLine("Successfully created an album with name '{0}' in the store", albumName);
            return albumName;
        }

        private string FetchAlbumIdFromName(string albumName)
        {
            Console.WriteLine("Fetching the album id of '{0}'", albumName);
            var response = httpClient.GetAsync(string.Format("/StoreManager/GetAlbumIdFromName?albumName={0}", albumName)).Result;
            ThrowIfResponseStatusNotOk(response);
            var albumId = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Album id for album '{0}' is '{1}'", albumName, albumId);
            return albumId;
        }

        private void VerifyAlbumDetails(string albumId, string albumName)
        {
            Console.WriteLine("Getting details of album with Id '{0}'", albumId);
            var response = httpClient.GetAsync(string.Format("/StoreManager/Details?id={0}", albumId)).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("http://myapp/testurl", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(string.Format("<a href=\"/StoreManager/Edit/{0}\">Edit</a>", albumId), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<a href=\"/StoreManager\">Back to List</a>", responseContent, StringComparison.OrdinalIgnoreCase);
        }

        private void AddAlbumToCart(string albumId, string albumName)
        {
            Console.WriteLine("Adding album id '{0}' to the cart", albumId);
            var response = httpClient.GetAsync(string.Format("/ShoppingCart/AddToCart?id={0}", albumId)).Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(albumName, responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<span class=\"glyphicon glyphicon glyphicon-shopping-cart\"></span>", responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Verified that album is added to cart");
        }

        private void CheckOutCartItems()
        {
            Console.WriteLine("Checking out the cart contents...");
            var response = httpClient.GetAsync("/Checkout/AddressAndPayment").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;

            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Checkout/AddressAndPayment")),
                    new KeyValuePair<string, string>("FirstName", "FirstNameValue"),
                    new KeyValuePair<string, string>("LastName", "LastNameValue"),
                    new KeyValuePair<string, string>("Address", "AddressValue"),
                    new KeyValuePair<string, string>("City", "Redmond"),
                    new KeyValuePair<string, string>("State", "WA"),
                    new KeyValuePair<string, string>("PostalCode", "98052"),
                    new KeyValuePair<string, string>("Country", "USA"),
                    new KeyValuePair<string, string>("Phone", "PhoneValue"),
                    new KeyValuePair<string, string>("Email", "email@email.com"),
                    new KeyValuePair<string, string>("PromoCode", "FREE"),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Checkout/AddressAndPayment", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<h2>Checkout Complete</h2>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(ApplicationBaseUrl + "Checkout/Complete/", response.RequestMessage.RequestUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteAlbum(string albumId, string albumName)
        {
            Console.WriteLine("Deleting album '{0}' from the store..", albumName);

            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("id", albumId)
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            var response = httpClient.PostAsync("/StoreManager/RemoveAlbum", content).Result;
            ThrowIfResponseStatusNotOk(response);

            Console.WriteLine("Verifying if the album '{0}' is deleted from store", albumName);
            response = httpClient.GetAsync(string.Format("/StoreManager/GetAlbumIdFromName?albumName={0}", albumName)).Result;
            Assert.Equal<HttpStatusCode>(HttpStatusCode.NotFound, response.StatusCode);
            Console.WriteLine("Album is successfully deleted from the store.", albumName, albumId);
        }

        private void ThrowIfResponseStatusNotOk(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);
                throw new Exception(string.Format("Received the above response with status code : {0}", response.StatusCode.ToString()));
            }
        }
    }
}