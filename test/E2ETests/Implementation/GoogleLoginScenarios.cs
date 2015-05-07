using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    public partial class Validator
    {
        public async Task LoginWithGoogle()
        {
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            var response = await _httpClient.GetAsync("Account/Login");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Signing in with Google account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Google"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await _httpClient.PostAsync("Account/ExternalLogin", content);
            Assert.Equal<string>("https://accounts.google.com/o/oauth2/auth", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = new ReadableStringCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal<string>("code", queryItems["response_type"]);
            Assert.Equal<string>("offline", queryItems["access_type"]);
            Assert.Equal<string>("[ClientId]", queryItems["client_id"]);
            Assert.Equal<string>(_deploymentResult.ApplicationBaseUri + "signin-google", queryItems["redirect_uri"]);
            Assert.Equal<string>("openid profile email", queryItems["scope"]);
            Assert.Equal<string>("ValidStateData", queryItems["state"]);
            Assert.Equal<string>("custom", queryItems["custom_redirect_uri"]);
            //Check for the correlation cookie
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Correlation.Google"));

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            response = await _httpClient.GetAsync("Account/Login");
            responseContent = await response.Content.ReadAsStringAsync();
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Google"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await _httpClient.PostAsync("Account/ExternalLogin", content);

            //Post a message to the Google middleware
            response = await _httpClient.GetAsync("signin-google?code=ValidCode&state=ValidStateData");
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            //Correlation cookie not getting cleared after successful signin?
            if (!Helpers.RunningOnMono)
            {
                Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Correlation.Google"));
            }
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Account/ExternalLoginCallback?ReturnUrl=%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains("AspnetvnextTest@gmail.com", responseContent, StringComparison.OrdinalIgnoreCase);

            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Email", "AspnetvnextTest@gmail.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLoginConfirmation?ReturnUrl=%2F")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await _httpClient.PostAsync("Account/ExternalLoginConfirmation", content);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains(string.Format("Hello {0}!", "AspnetvnextTest@gmail.com"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.ExternalLogin"));
            _logger.LogInformation("Successfully signed in with user '{email}'", "AspnetvnextTest@gmail.com");

            _logger.LogInformation("Verifying if the middleware notifications were fired");
            //Check for a non existing item
            response = await _httpClient.GetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", "123"));
            //This action requires admin permissions. If notifications are fired this permission is granted
            _logger.LogVerbose(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Middleware notifications were fired successfully");
        }
    }
}