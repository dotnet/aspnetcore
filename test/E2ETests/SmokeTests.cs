using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using Xunit;

namespace E2ETests
{
    public class SmokeTests
    {
        private string ApplicationBaseUrl = null;
        private const string Connection_string_Format = "Server=(localdb)\\v11.0;Database={0};Trusted_Connection=True;MultipleActiveResultSets=true";

        [Theory]
        [InlineData(HostType.Helios, KreFlavor.DesktopClr, "http://localhost:5001/")]
        //[InlineData(HostType.SelfHost, KreFlavor.DesktopClr, "http://localhost:5002/")]
        public void SmokeTestSuite(HostType hostType, KreFlavor kreFlavor, string applicationBaseUrl)
        {
            var musicStoreDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var musicStoreIdentityDbName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            Console.WriteLine("Pointing MusicStore DB to '{0}'", string.Format(Connection_string_Format, musicStoreDbName));
            Console.WriteLine("Pointing MusicStoreIdentity DB to '{0}'", string.Format(Connection_string_Format, musicStoreIdentityDbName));

            Environment.SetEnvironmentVariable("SQLAZURECONNSTR_DefaultConnection", string.Format(Connection_string_Format, musicStoreDbName));
            Environment.SetEnvironmentVariable("SQLAZURECONNSTR_IdentityConnection", string.Format(Connection_string_Format, musicStoreIdentityDbName));

            ApplicationBaseUrl = applicationBaseUrl;
            var hostProcess = DeploymentUtility.StartApplication(hostType, kreFlavor);

            try
            {
                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                //Request to base address and check if various parts of the body are rendered
                VerifyHomePage(httpClient);

                //Making a request to a protected resource should automatically redirect to login page
                AccessStoreWithoutPermissions(httpClient);

                //Register a user - Negative scenario where the Password & ConfirmPassword do not match
                RegisterUserWithNonMatchingPasswords(httpClient, httpClientHandler);

                //Register a valid user
                var generatedUserName = RegisterValidUser(httpClient, httpClientHandler);

                //Register a user - Negative scenario : Trying to register a user name that's already registered.
                RegisterExistingUser(httpClient, httpClientHandler, generatedUserName);

                //Logout from this user session - This should take back to the home page
                SignOutUser(httpClient, httpClientHandler, generatedUserName);

                //Sign in scenarios: Invalid password - Expected an invalid user name password error.
                SignInWithInvalidPassword(httpClient, httpClientHandler, generatedUserName);

                //Sign in scenarios: Valid user name & password.
                SignInWithUser(httpClient, httpClientHandler, generatedUserName, "Password~1");

                //Change password scenario
                ChangePassword(httpClient, httpClientHandler, generatedUserName);

                //Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                AccessStoreWithoutPermissions(httpClient, generatedUserName);

                //Logout from this user session - This should take back to the home page
                SignOutUser(httpClient, httpClientHandler, generatedUserName);

                //Login as an admin user
                SignInWithUser(httpClient, httpClientHandler, "Administrator", "YouShouldChangeThisPassword1!");

                //Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                AccessStoreWithPermissions(httpClient);

                //Create an album
                CreateAlbum(httpClient, httpClientHandler);

                //Logout from this user session - This should take back to the home page
                SignOutUser(httpClient, httpClientHandler, "Administrator");
            }
            finally
            {
                //Shutdown the host process
                hostProcess.Kill();

                try
                {
                    Console.WriteLine("Trying to drop the databases created during the test run");
                    using (var conn = new SqlConnection(@"Server=(localdb)\v11.0;Database=master;Trusted_Connection=True;"))
                    {
                        conn.Open();
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = string.Format("DROP DATABASE {0}", musicStoreDbName);
                        cmd.ExecuteNonQuery();

                        cmd = conn.CreateCommand();
                        cmd.CommandText = string.Format("DROP DATABASE {0}", musicStoreIdentityDbName);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception exception)
                {
                    //Ignore if there is failure in cleanup.
                    Console.WriteLine("Error occured while dropping the databases", exception);
                }
            }
        }

        private void VerifyHomePage(HttpClient httpClient)
        {
            var response = httpClient.GetAsync(string.Empty).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Home page content : {0}", responseContent);
            Assert.Equal<HttpStatusCode>(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li><a href=\"/\">Home</a></li>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<a class=\"dropdown-toggle\" data-toggle=\"dropdown\">Store <b class=\"caret\"></b></a>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<ul class=\"dropdown-menu\">", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<li class=\"divider\"></li>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<a href=\"/Store/Details/", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Application initialization successful.");
        }

        private void AccessStoreWithoutPermissions(HttpClient httpClient, string generatedUserName = null)
        {
            Console.WriteLine("Trying to access StoreManager that needs ManageStore claim with the current user : {0}", generatedUserName ?? "Anonymous");
            var response = httpClient.GetAsync("/StoreManager/").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Equal<string>(ApplicationBaseUrl + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            Console.WriteLine("Redirected to login page as expected.");
        }

        private void AccessStoreWithPermissions(HttpClient httpClient)
        {
            Console.WriteLine("Trying to access the store inventory..");
            var response = httpClient.GetAsync("/StoreManager/").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(ApplicationBaseUrl + "StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);
            Console.WriteLine("Successfully acccessed the store inventory");
        }

        private void RegisterUserWithNonMatchingPasswords(HttpClient httpClient, HttpClientHandler httpClientHandler)
        {
            Console.WriteLine("Trying to create user with not matching password and confirm password");
            var response = httpClient.GetAsync("/Account/Register").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

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
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Assert.Contains("The password and confirmation password do not match.", responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Server side model validator rejected the user '{0}''s registration as passwords do not match.", generatedUserName);
        }

        private string RegisterValidUser(HttpClient httpClient, HttpClientHandler httpClientHandler)
        {
            var response = httpClient.GetAsync("/Account/Register").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

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
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Console.WriteLine("Successfully registered user '{0}' and signed in", generatedUserName);
            return generatedUserName;
        }

        private void RegisterExistingUser(HttpClient httpClient, HttpClientHandler httpClientHandler, string generatedUserName)
        {
            Console.WriteLine("Trying to register a user with name '{0}' again", generatedUserName);
            var response = httpClient.GetAsync("/Account/Register").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
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
            //Bug? Registering the same user again does not throw this error
            Assert.Contains(string.Format("Name {0} is already taken.", generatedUserName), responseContent, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine("Identity threw a valid exception that user '{0}' already exists in the system", generatedUserName);
        }

        private void SignOutUser(HttpClient httpClient, HttpClientHandler httpClientHandler, string generatedUserName)
        {
            Console.WriteLine("Signing out from '{0}''s session", generatedUserName);
            var response = httpClient.GetAsync(string.Empty).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
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
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Console.WriteLine("Successfully signed out of '{0}''s session", generatedUserName);
        }

        private void SignInWithInvalidPassword(HttpClient httpClient, HttpClientHandler httpClientHandler, string generatedUserName)
        {
            var response = httpClient.GetAsync("/Account/Login").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with user '{0}'", generatedUserName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", generatedUserName),
                    new KeyValuePair<string, string>("Password", "InvalidPassword~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains("<div class=\"validation-summary-errors\"><ul><li>Invalid username or password.</li>", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie not sent
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Console.WriteLine("Identity successfully prevented an invalid user login.");
        }

        private void SignInWithUser(HttpClient httpClient, HttpClientHandler httpClientHandler, string generatedUserName, string password)
        {
            var response = httpClient.GetAsync("/Account/Login").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with user '{0}'", generatedUserName);
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", generatedUserName),
                    new KeyValuePair<string, string>("Password", password),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/Account/Login", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Contains(string.Format("Hello {0}!", generatedUserName), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Console.WriteLine("Successfully signed in with user '{0}'", generatedUserName);
        }

        private void ChangePassword(HttpClient httpClient, HttpClientHandler httpClientHandler, string generatedUserName)
        {
            var response = httpClient.GetAsync("/Account/Manage").Result;
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
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            Console.WriteLine("Successfully changed the password for user '{0}'", generatedUserName);
        }

        private void CreateAlbum(HttpClient httpClient, HttpClientHandler httpClientHandler)
        {
            var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
            Console.WriteLine("Trying to create an album with name '{0}'", albumName);
            var response = httpClient.GetAsync("/StoreManager/create").Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/StoreManager/create")),
                    new KeyValuePair<string, string>("GenreId", "1"),
                    new KeyValuePair<string, string>("ArtistId", "1"),
                    new KeyValuePair<string, string>("Title", albumName),
                    new KeyValuePair<string, string>("Price", "9.99"),
                    new KeyValuePair<string, string>("AlbumArtUrl", "TestUrl"),
                };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("/StoreManager/create", content).Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            Assert.Equal<string>(ApplicationBaseUrl + "StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains(albumName, responseContent);
            Console.WriteLine("Successfully created an album with name '{0}' in the store", albumName);
        }
    }
}