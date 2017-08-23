using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace E2ETests
{
    public partial class Validator
    {
        public async Task LoginWithGoogle()
        {
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            var response = await DoGetAsync("Account/Login");
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
            response = await DoPostAsync("Account/ExternalLogin", content);
            Assert.Equal("https://accounts.google.com/o/oauth2/auth", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = new QueryCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal("code", queryItems["response_type"]);
            Assert.Equal("offline", queryItems["access_type"]);
            Assert.Equal("[ClientId]", queryItems["client_id"]);
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "signin-google", queryItems["redirect_uri"]);
            Assert.Equal("openid profile email", queryItems["scope"]);
            Assert.Equal("ValidStateData", queryItems["state"]);
            Assert.Equal("custom", queryItems["custom_redirect_uri"]);
            //Check for the correlation cookie
            // Workaround for https://github.com/dotnet/corefx/issues/21250
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));
            var setCookie = SetCookieHeaderValue.ParseList(setCookieValues.ToList());
            Assert.Contains(setCookie, c => c.Name.StartsWith(".AspNetCore.Correlation.Google", StringComparison.OrdinalIgnoreCase));

            // This is just enable the auto-redirect.
            _httpClientHandler = new HttpClientHandler();
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };
            foreach (var header in SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList()))
            {
                // Workaround for https://github.com/dotnet/corefx/issues/21250
                // The path of the cookie must either match the URI or be a prefix of it due to the fact
                // that CookieContainer doesn't support the latest version of the standard for cookies.
                var uri = new Uri(new Uri(_deploymentResult.ApplicationBaseUri), header.Path.ToString());
                _httpClientHandler.CookieContainer.Add(uri, new Cookie(header.Name.ToString(), header.Value.ToString()));
            }

            //Post a message to the Google middleware
            response = await DoGetAsync("signin-google?code=ValidCode&state=ValidStateData");
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            //Correlation cookie not getting cleared after successful signin?
            Assert.DoesNotContain(".AspNetCore.Correlation.Google", GetCookieNames(_deploymentResult.ApplicationBaseUri + "signin-google"));

            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Account/ExternalLoginCallback?ReturnUrl=%2F", response.RequestMessage.RequestUri.AbsoluteUri);
            Assert.Contains("AspnetvnextTest@gmail.com", responseContent, StringComparison.OrdinalIgnoreCase);

            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Email", "AspnetvnextTest@gmail.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLoginConfirmation?ReturnUrl=%2F")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await DoPostAsync("Account/ExternalLoginConfirmation", content);

            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains(string.Format("Hello {0}!", "AspnetvnextTest@gmail.com"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            // Verify cookie sent
            Assert.Contains(IdentityCookieName, GetCookieNames());
            Assert.DoesNotContain(ExternalLoginCookieName, GetCookieNames());

            _logger.LogInformation("Successfully signed in with user '{email}'", "AspnetvnextTest@gmail.com");

            _logger.LogInformation("Verifying if the middleware events were fired");
            //Check for a non existing item
            response = await DoGetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", "123"));
            //This action requires admin permissions. If events are fired this permission is granted
            _logger.LogDebug(await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Middleware events were fired successfully");
        }
    }
}
