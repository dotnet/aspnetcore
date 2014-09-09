using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private void LoginWithFacebook()
        {
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            var response = httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Signing in with Facebook account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Facebook"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;
            Assert.Equal<string>("https://www.facebook.com/dialog/oauth", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = response.Headers.Location.ParseQueryString();
            Assert.Equal<string>("code", queryItems["response_type"]);
            Assert.Equal<string>("[AppId]", queryItems["client_id"]);
            Assert.Equal<string>(ApplicationBaseUrl + "signin-facebook", queryItems["redirect_uri"]);
            Assert.Equal<string>("email,read_friendlists,user_checkins", queryItems["scope"]);
            Assert.Equal<string>("ValidStateData", queryItems["state"]);
            Assert.Equal<string>("custom", queryItems["custom_redirect_uri"]);
            //Check for the correlation cookie
            Assert.NotNull(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Correlation.Facebook"));

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(ApplicationBaseUrl) };

            response = httpClient.GetAsync("Account/Login").Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Facebook"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = httpClient.PostAsync("Account/ExternalLogin", content).Result;

            //Post a message to the Facebook middleware
            response = httpClient.GetAsync("signin-facebook?code=ValidCode&state=ValidStateData").Result;
            //This should land us in ExternalLoginCallBack - this action is not implemented yet. We need to wait to complete automation.

            //Correlation cookie not getting cleared after successful signin?
            //Assert.Null(httpClientHandler.CookieContainer.GetCookies(new Uri(ApplicationBaseUrl)).GetCookieWithName(".AspNet.Correlation.Facebook"));
        }
    }
}