using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Xunit;

namespace E2ETests
{
    /// <summary>
    /// Summary description for TwitterLoginScenarios
    /// </summary>
    public partial class SmokeTests
    {
        private void LoginWithTwitter()
        {
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            var response = httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with Twitter account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Twitter"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;
            Assert.Equal<string>("https://twitter.com/oauth/authenticate", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = response.Headers.Location.ParseQueryString();
            Assert.Equal<string>("custom", queryItems["custom_redirect_uri"]);
            Assert.Equal<string>("valid_oauth_token", queryItems["oauth_token"]);
            //Check for the correlation cookie
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl))["__TwitterState"]);

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            response = httpClient.GetAsync("Account/Login").Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Twitter"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;

            //Post a message to the Facebook middleware
            response = httpClient.GetAsync("signin-twitter?oauth_token=valid_oauth_token&oauth_verifier=valid_oauth_verifier").Result;
            //This should land us in ExternalLoginCallBack - this action is not implemented yet. We need to wait to complete automation.

            //Correlation cookie not getting cleared after successful signin?
            //Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl))["__TwitterState"]);
        }
    }
}