using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace E2ETests
{
    public class SmokeTests
    {
        [Theory]
        [InlineData(HostType.Helios, KreFlavor.DesktopClr, "http://localhost:5001/")]
        //[InlineData(HostType.SelfHost, KreFlavor.DesktopClr, "http://localhost:5002/")]
        public void SmokeTestSuite(HostType hostType, KreFlavor kreFlavor, string applicationBaseUrl)
        {
            var hostProcess = DeploymentUtility.StartApplication(hostType, kreFlavor);

            try
            {
                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(applicationBaseUrl) };

                //Request to base address and check if various parts of the body are rendered
                var response = httpClient.GetAsync(string.Empty).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Home page content : {0}", responseContent);
                Assert.Equal<HttpStatusCode>(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine("Application initialization successful.");

                //Making a request to a protected resource should automatically redirect to login page
                Console.WriteLine("Trying to access StoreManager without signing in..");
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Equal<string>(applicationBaseUrl + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);
                Console.WriteLine("Redirected to login page as expected.");

                //Register a user - Need a way to get the antiforgery token and send it in the request as a form encoded parameter
                response = httpClient.GetAsync("/Account/Register").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;

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
                Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
                Console.WriteLine("Successfully registered user '{0}' and signed in", generatedUserName);

                //Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                Console.WriteLine("Trying to access StoreManager that needs special permissions that {0} does not claim", generatedUserName);
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Equal<string>(applicationBaseUrl + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);
                Console.WriteLine("Redirected to login page as expected.");

                //Logout from this user session - This should take back to the home page
                Console.WriteLine("Signing out from '{0}''s session", generatedUserName);
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/Account/LogOff", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
                //Verify cookie cleared on logout
                Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
                Console.WriteLine("Successfully signed out of '{0}''s session", generatedUserName);

                //Login as an admin user
                Console.WriteLine("Signing in as '{0}'", "Administrator");
                response = httpClient.GetAsync("/Account/Login").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", "Administrator"),
                    new KeyValuePair<string, string>("Password", "YouShouldChangeThisPassword1!"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/Login")),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/Account/Login", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains(string.Format("Hello {0}!", "Administrator"), responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine("Successfully signed in as '{0}'", "Administrator");

                //Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                Console.WriteLine("Trying to access the store inventory..");
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Equal<string>(applicationBaseUrl + "StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);
                Console.WriteLine("Successfully acccessed the store inventory");

                //Create an album
                var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                Console.WriteLine("Trying to create an album with name '{0}'", albumName);
                response = httpClient.GetAsync("/StoreManager/create").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/StoreManager/create")),
                    new KeyValuePair<string, string>("GenreId", "1"),
                    new KeyValuePair<string, string>("ArtistId", "1"),
                    new KeyValuePair<string, string>("Title", albumName),
                    new KeyValuePair<string, string>("Price", "9.99"),
                    new KeyValuePair<string, string>("AlbumArtUrl", "TestUrl"),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/StoreManager/create", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Equal<string>(applicationBaseUrl + "StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
                Assert.Contains(albumName, responseContent);
                Console.WriteLine("Successfully created an album with name '{0}' in the store", albumName);

                //Logout from this user session - This should take back to the home page
                Console.WriteLine("Signing out of '{0}''s session", "Administrator");
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/Account/LogOff", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);
                //Verify cookie cleared on logout
                Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(applicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Security.Application"));
                Console.WriteLine("Successfully signed out of '{0}''s session", "Administrator");
            }
            finally
            {
                //Shutdown the host process
                hostProcess.Kill();
            }
        }
    }
}