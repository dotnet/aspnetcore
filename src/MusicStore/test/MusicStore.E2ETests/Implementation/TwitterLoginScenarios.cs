using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Xunit;

namespace E2ETests
{
    /// <summary>
    /// Summary description for TwitterLoginScenarios
    /// </summary>
    public partial class Validator
    {
        public async Task LoginWithTwitter()
        {
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            var response = await DoGetAsync("Account/Login");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Signing in with Twitter account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Twitter"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/ExternalLogin", content);
            Assert.StartsWith("https://api.twitter.com/oauth/authenticate", response.Headers.Location.ToString());
            var queryItems = new QueryCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal("custom", queryItems["custom_redirect_uri"]);
            Assert.Equal("valid_oauth_token", queryItems["oauth_token"]);
            //Check for the correlation cookie
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri))["__TwitterState"]);

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            response = await DoGetAsync("Account/Login");
            responseContent = await response.Content.ReadAsStringAsync();
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "Twitter"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/ExternalLogin", content);
            //Post a message to the Facebook middleware
            response = await DoGetAsync("signin-twitter?oauth_token=valid_oauth_token&oauth_verifier=valid_oauth_verifier");
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            //Check correlation cookie not getting cleared after successful signin
            Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri))["__TwitterState"]);
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Account/ExternalLoginCallback?ReturnUrl=%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            //Twitter does not give back the email claim for some reason.
            //Assert.Contains("AspnetvnextTest@gmail.com", responseContent, StringComparison.OrdinalIgnoreCase);

            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Email", "twitter@test.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLoginConfirmation?ReturnUrl=%2F")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/ExternalLoginConfirmation", content);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains(string.Format("Hello {0}!", "twitter@test.com"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            // Verify cookie sent
            Assert.Contains(IdentityCookieName, GetCookieNames());
            Assert.DoesNotContain(ExternalLoginCookieName, GetCookieNames());
            _logger.LogInformation("Successfully signed in with user '{email}'", "twitter@test.com");

            _logger.LogInformation("Verifying if the middleware events were fired");
            //Check for a non existing item
            response = await DoGetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", "123"));
            //This action requires admin permissions. If events are fired this permission is granted
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Middleware events were fired successfully");
        }
    }
}