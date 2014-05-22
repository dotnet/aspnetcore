using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Xunit;

namespace E2ETests
{
    public class SmokeTests
    {
        private const string APP_BASE_URL = "http://localhost:5001/";
        private const string APP_RELATIVE_PATH = @"..\..\src\MusicStore\";

        [Fact]
        public void SmokeTestSuite()
        {
            string applicationPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, APP_RELATIVE_PATH));
            Utility.CopyAspNetLoader(applicationPath);
            var hostProcess = Utility.StartHeliosHost(applicationPath);

            try
            {
                var httpClientHandler = new HttpClientHandler();
                var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(APP_BASE_URL) };

                //Request to base address and check if various parts of the body are rendered
                var response = httpClient.GetAsync(string.Empty).Result;
                var responseContent = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Response from the server: {0}", responseContent);
                Assert.Equal<HttpStatusCode>(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("ASP.NET MVC Music Store", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Register", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Login", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("mvcmusicstore.codeplex.com", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("/Images/home-showcase.png", responseContent, StringComparison.OrdinalIgnoreCase);

                //Making a request to a protected resource should automatically redirect to login page
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Equal<string>(APP_BASE_URL + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);

                //Register a user - Need a way to get the antiforgery token and send it in the request as a form encoded parameter
                response = httpClient.GetAsync("/Account/Register").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                var antiForgeryToken = Utility.RetrieveAntiForgeryToken(responseContent, "/Account/Register");

                var generatedUserName = Guid.NewGuid().ToString().Replace("-", string.Empty);
                var formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", generatedUserName),
                    new KeyValuePair<string, string>("Password", "Password~1"),
                    new KeyValuePair<string, string>("ConfirmPassword", "Password~1"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
                };

                var content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/Account/Register", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains(string.Format("Hello {0}!", generatedUserName), responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);

                //Verify cookie sent
                Assert.NotNull(Utility.GetCookieWithName(httpClientHandler.CookieContainer.GetCookies(new Uri(APP_BASE_URL)), ".AspNet.Microsoft.AspNet.Identity.Security.Application"));

                //Making a request to a protected resource that this user does not have access to - should automatically redirect to login page again
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains("<h4>Use a local account to log in.</h4>", responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Equal<string>(APP_BASE_URL + "Account/Login?ReturnUrl=%2FStoreManager%2F", response.RequestMessage.RequestUri.AbsoluteUri);

                //Logout from this user session - This should take back to the home page
                antiForgeryToken = Utility.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff");
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
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
                Assert.Null(Utility.GetCookieWithName(httpClientHandler.CookieContainer.GetCookies(new Uri(APP_BASE_URL)), ".AspNet.Microsoft.AspNet.Identity.Security.Application"));

                //Login as an admin user
                response = httpClient.GetAsync("/Account/Login").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                antiForgeryToken = Utility.RetrieveAntiForgeryToken(responseContent, "/Account/Login");
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("UserName", "Administrator"),
                    new KeyValuePair<string, string>("Password", "YouShouldChangeThisPassword1!"),
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/Account/Login", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Contains(string.Format("Hello {0}!", "Administrator"), responseContent, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);

                //Now navigating to the store manager should work fine as this user has the necessary permission to administer the store.
                response = httpClient.GetAsync("/StoreManager/").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Equal<string>(APP_BASE_URL + "StoreManager/", response.RequestMessage.RequestUri.AbsoluteUri);

                //Create an album
                var albumName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                response = httpClient.GetAsync("/StoreManager/create").Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                antiForgeryToken = Utility.RetrieveAntiForgeryToken(responseContent, "/StoreManager/create");
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
                    new KeyValuePair<string, string>("GenreId", "1"),
                    new KeyValuePair<string, string>("ArtistId", "1"),
                    new KeyValuePair<string, string>("Title", albumName),
                    new KeyValuePair<string, string>("Price", "9.99"),
                    new KeyValuePair<string, string>("AlbumArtUrl", "TestUrl"),
                };

                content = new FormUrlEncodedContent(formParameters.ToArray());
                response = httpClient.PostAsync("/StoreManager/create", content).Result;
                responseContent = response.Content.ReadAsStringAsync().Result;
                Assert.Equal<string>(APP_BASE_URL + "StoreManager", response.RequestMessage.RequestUri.AbsoluteUri);
                Assert.Contains(albumName, responseContent);

                //Logout from this user session - This should take back to the home page
                antiForgeryToken = Utility.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff");
                formParameters = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken),
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
                Assert.Null(Utility.GetCookieWithName(httpClientHandler.CookieContainer.GetCookies(new Uri(APP_BASE_URL)), ".AspNet.Microsoft.AspNet.Identity.Security.Application"));
            }
            finally
            {
                //Shutdown the host process
                hostProcess.Kill();
            }
        }
    }
}