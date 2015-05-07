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
        public async Task LoginWithOpenIdConnect()
        {
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            var response = await _httpClient.GetAsync("Account/Login");
            await ThrowIfResponseStatusNotOk(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Signing in with OpenIdConnect account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "OpenIdConnect"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await _httpClient.PostAsync("Account/ExternalLogin", content);
            Assert.Equal<string>("https://login.windows.net/4afbc689-805b-48cf-a24c-d4aa3248a248/oauth2/authorize", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = new ReadableStringCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal<string>("c99497aa-3ee2-4707-b8a8-c33f51323fef", queryItems["client_id"]);
            Assert.Equal<string>("form_post", queryItems["response_mode"]);
            Assert.Equal<string>("code id_token", queryItems["response_type"]);
            Assert.Equal<string>("openid profile", queryItems["scope"]);
            Assert.Equal<string>("OpenIdConnect.AuthenticationProperties=ValidStateData", queryItems["state"]);
            Assert.NotNull(queryItems["nonce"]);
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.OpenIdConnect.Nonce.protectedString"));

            // This is just enable the auto-redirect.
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };
            _httpClientHandler.CookieContainer.Add(new Uri(_deploymentResult.ApplicationBaseUri), new Cookie(".AspNet.OpenIdConnect.Nonce.protectedString", "N"));

            //Post a message to the OpenIdConnect middleware
            var token = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("code", "AAABAAAAvPM1KaPlrEqdFSBzjqfTGGBtrTYVn589oKw4lLgJ6Svz0AhPVOJr0J2-Uu_KffGlqIbYlRAyxmt-vZ7VlSVdrWvOkNhK9OaAMaSD7LDoPbBTVMEkB0MdAgBTV34l2el-s8ZI02_9PvgQaORZs7n8eGaGbcoKAoxiDn2OcKuJVplXYgrGUwU4VpRaqe6RaNzuseM7qBFbLIv4Wps8CndE6W8ccmuu6EvGC6-H4uF9EZL7gU4nEcTcvkE4Qyt8do6VhTVfM1ygRNQgmV1BCig5t_5xfhL6-xWQdy15Uzn_Df8VSsyDXe8s9cxyKlqc_AIyLFy_NEiMQFUqjZWKd_rR3A8ugug15SEEGuo1kF3jMc7dVMdE6OF9UBd-Ax5ILWT7V4clnRQb6-CXB538DlolREfE-PowXYruFBA-ARD6rwAVtuVfCSbS0Zr4ZqfNjt6x8yQdK-OkdQRZ1thiZcZlm1lyb2EquGZ8Deh2iWBoY1uNcyjzhG-L43EivxtHAp6Y8cErhbo41iacgqOycgyJWxiB5J0HHkxD0nQ2RVVuY8Ybc9sdgyfKkkK2wZ3idGaRCdZN8Q9VBhWRXPDMqHWG8t3aZRtvJ_Xd3WhjNPJC0GpepUGNNQtXiEoIECC363o1z6PZC5-E7U3l9xK06BZkcfTOnggUiSWNCrxUKS44dNqaozdYlO5E028UgAEhJ4eDtcP3PZty-0j4j5Mw0F2FmyAA"),
                new KeyValuePair<string, string>("id_token", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtyaU1QZG1Cdng2OHNrVDgtbVBBQjNCc2VlQSJ9.eyJhdWQiOiJjOTk0OTdhYS0zZWUyLTQ3MDctYjhhOC1jMzNmNTEzMjNmZWYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC80YWZiYzY4OS04MDViLTQ4Y2YtYTI0Yy1kNGFhMzI0OGEyNDgvIiwiaWF0IjoxNDIyMzk1NzYzLCJuYmYiOjE0MjIzOTU3NjMsImV4cCI6MTQyMjM5OTY2MywidmVyIjoiMS4wIiwidGlkIjoiNGFmYmM2ODktODA1Yi00OGNmLWEyNGMtZDRhYTMyNDhhMjQ4IiwiYW1yIjpbInB3ZCJdLCJvaWQiOiJmODc2YWJlYi1kNmI1LTQ0ZTQtOTcxNi02MjY2YWMwMTgxYTgiLCJ1cG4iOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IlBVZGhjbFA1UGdJalNVOVAxUy1IZWxEYVNGU2YtbVhWMVk2MC1LMnZXcXciLCJnaXZlbl9uYW1lIjoiVXNlcjMiLCJmYW1pbHlfbmFtZSI6IlVzZXIzIiwibmFtZSI6IlVzZXIzIiwidW5pcXVlX25hbWUiOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsIm5vbmNlIjoiNjM1NTc5OTI4NjM5NTE3NzE1Lk9UUmpPVFZrTTJFdE1EUm1ZUzAwWkRFM0xUaGhaR1V0WldabVpHTTRPRGt6Wkdaa01EUmxORGhrTjJNdE9XSXdNQzAwWm1Wa0xXSTVNVEl0TVRVd1ltUTRNemRtT1dJMCIsImNfaGFzaCI6IkZHdDN3Y1FBRGUwUFkxUXg3TzFyNmciLCJwd2RfZXhwIjoiNjY5MzI4MCIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgifQ.coAdCkdMgnslMHagdU8IBgH7Z0dilRdMfKytyqPJuTr6sbmbhrAoAj-KeGwbKgzrd-BeDk_rW47dntWuuAqGrAOGzxXvS2dcSWgoEKoXuDccIL5b4rIomRpfJpaeE-YwiU3usyRvoQCpHmtOa0g7xVilIj3_1-9ylMgRDY5qcrtQ_hEZlGuYyiCPR0dw8WmNU7r6PKObG-o3Yk_RbEBHjnaWxKoJwrVUEZUQOJDAvlr6ZYEmGTlD_BM0Rc_0fJZPU7A3uN9PHLw1atm-chN06IDXf23R33JI_xFuEZnj9HZQ_eIzNCl7GFmUryK3FFgYJpIbsI0BIFuksSikXz33IA"),
                new KeyValuePair<string, string>("state", "OpenIdConnect.AuthenticationProperties=ValidStateData"),
                new KeyValuePair<string, string>("session_state", "d0b59ffa-2df9-4d8c-b43a-2c410987f4ae")
            };

            response = await _httpClient.PostAsync(string.Empty, new FormUrlEncodedContent(token.ToArray()));
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(_deploymentResult.ApplicationBaseUri + "Account/ExternalLoginCallback?ReturnUrl=%2F", response.RequestMessage.RequestUri.AbsoluteUri);

            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Email", "User3@aspnettest.onmicrosoft.com"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLoginConfirmation?ReturnUrl=%2F")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = await _httpClient.PostAsync("Account/ExternalLoginConfirmation", content);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();

            Assert.Contains(string.Format("Hello {0}!", "User3@aspnettest.onmicrosoft.com"), responseContent, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Log off", responseContent, StringComparison.OrdinalIgnoreCase);
            //Verify cookie sent
            Assert.NotNull(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Null(_httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.ExternalLogin"));
            _logger.LogInformation("Successfully signed in with user '{email}'", "User3@aspnettest.onmicrosoft.com");

            _logger.LogInformation("Verifying if the middleware notifications were fired");
            //Check for a non existing item
            response = await _httpClient.GetAsync(string.Format("Admin/StoreManager/GetAlbumIdFromName?albumName={0}", "123"));
            //This action requires admin permissions. If notifications are fired this permission is granted
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _logger.LogInformation("Middleware notifications were fired successfully");

            _logger.LogInformation("Verifying the OpenIdConnect logout flow..");
            response = await _httpClient.GetAsync(string.Empty);
            await ThrowIfResponseStatusNotOk(response);
            responseContent = await response.Content.ReadAsStringAsync();
            ValidateLayoutPage(responseContent);
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/LogOff")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            // Need a non-redirecting handler
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            handler.CookieContainer.Add(new Uri(_deploymentResult.ApplicationBaseUri), _httpClientHandler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)));
            _httpClient = new HttpClient(handler) { BaseAddress = new Uri(_deploymentResult.ApplicationBaseUri) };

            response = await _httpClient.PostAsync("Account/LogOff", content);
            Assert.Null(handler.CookieContainer.GetCookies(new Uri(_deploymentResult.ApplicationBaseUri)).GetCookieWithName(".AspNet.Microsoft.AspNet.Identity.Application"));
            Assert.Equal<string>(
                "https://login.windows.net/4afbc689-805b-48cf-a24c-d4aa3248a248/oauth2/logout",
                response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            queryItems = new ReadableStringCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal<string>(_deploymentResult.ApplicationBaseUri + "Account/Login", queryItems["post_logout_redirect_uri"]);
        }
    }
}