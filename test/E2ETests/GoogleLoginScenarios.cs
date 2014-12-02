using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Xunit;
using Microsoft.AspNet.WebUtilities;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private void LoginWithGoogle()
        {
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            var response = httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with Google account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Google"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;
            Assert.Equal<string>("https://accounts.google.com/o/oauth2/auth", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = QueryHelpers.ParseQuery(response.Headers.Location.Query);
            Assert.Equal<string>("code", queryItems["response_type"]);
            Assert.Equal<string>("offline", queryItems["access_type"]);
            Assert.Equal<string>("[ClientId]", queryItems["client_id"]);
            Assert.Equal<string>(ApplicationBaseUrl + "signin-google", queryItems["redirect_uri"]);
            Assert.Equal<string>("openid profile email", queryItems["scope"]);
            Assert.Equal<string>("ValidStateData", queryItems["state"]);
            Assert.Equal<string>("custom", queryItems["custom_redirect_uri"]);
            //Check for the correlation cookie
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Correlation.Google"));

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            response = httpClient.GetAsync("Account/Login").Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Google"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;

            //Post a message to the Google middleware
            response = httpClient.GetAsync("signin-google?code=ValidCode&state=ValidStateData").Result;
            ThrowIfResponseStatusNotOk(response);
            responseContent = response.Content.ReadAsStringAsync().Result;

            //Correlation cookie not getting cleared after successful signin?
            if (!Helpers.RunningOnMono)
            {
                Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Correlation.Google")); 
            }
            Assert.Equal(ApplicationBaseUrl + "Account/ExternalLoginCallback?ReturnUrl=%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains("AspnetvnextTest@gmail.com", responseContent, StringComparison.OrdinalIgnoreCase);

            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Email", "AspnetvnextTest@gmail.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLoginConfirmation?ReturnUrl=%2F")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLoginConfirmation", content).Result;
            ThrowIfResponseStatusNotOk(response);
            responseContent = response.Content.ReadAsStringAsync().Result;

            Assert.Contains(string.Format("Hello {0}!", "AspnetvnextTest@gmail.com"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.ExternalLogin"));
            Console.WriteLine("Successfully signed in with user '{0}'", "AspnetvnextTest@gmail.com");

            Console.WriteLine("Verifying if the middleware notifications were fired");
            //Check for a non existing item
            response = httpClient.GetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", "123")).Result;
            //This action requires admin permissions. If notifications are fired this permission is granted
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Console.WriteLine("Middleware notifications were fired successfully");
        }
    }
}