using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Logging;
using Xunit;

namespace E2ETests
{
    public partial class SmokeTests
    {
        private void LoginWithOpenIdConnect()
        {
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_applicationBaseUrl) };

            var response = _httpClient.GetAsync("Account/Login").Result;
            ThrowIfResponseStatusNotOk(response);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.WriteInformation("Signing in with OpenIdConnect account");
            var formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "OpenIdConnect"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            var content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/ExternalLogin", content).Result;
            Assert.Equal<string>("https://login.windows.net/4afbc689-805b-48cf-a24c-d4aa3248a248/oauth2/authorize", response.Headers.Location.AbsoluteUri.Replace(response.Headers.Location.Query, string.Empty));
            var queryItems = new ReadableStringCollection(QueryHelpers.ParseQuery(response.Headers.Location.Query));
            Assert.Equal<string>("c99497aa-3ee2-4707-b8a8-c33f51323fef", queryItems["client_id"]);
            Assert.Equal<string>("form_post", queryItems["response_mode"]);
            Assert.Equal<string>("code id_token", queryItems["response_type"]);
            Assert.Equal<string>("openid profile", queryItems["scope"]);
            Assert.Equal<string>("OpenIdConnect.AuthenticationProperties=ValidStateData", queryItems["state"]);

            //This is just to generate a correlation cookie. Previous step would generate this cookie, but we have reset the handler now.
            _httpClientHandler = new HttpClientHandler() { AllowAutoRedirect = true };
            _httpClient = new HttpClient(_httpClientHandler) { BaseAddress = new Uri(_applicationBaseUrl) };

            response = _httpClient.GetAsync("Account/Login").Result;
            responseContent = response.Content.ReadAsStringAsync().Result;
            formParameters = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("provider", "OpenIdConnect"),
                new KeyValuePair<string, string>("returnUrl", "/"),
                new KeyValuePair<string, string>("__RequestVerificationToken", HtmlDOMHelper.RetrieveAntiForgeryToken(responseContent, "/Account/ExternalLogin")),
            };

            content = new FormUrlEncodedContent(formParameters.ToArray());
            response = _httpClient.PostAsync("Account/ExternalLogin", content).Result;

            //Post a message to the OpenIdConnect middleware
            var token = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("code", "AAABAAAAvPM1KaPlrEqdFSBzjqfTGMQtbI_OHOamje5gJL8fAgpLsNlGHTJmFBHKtpy8zM9Ck__IcUuEd7oirpHPB6yhq2m6e-hjLiJv1AcHNR8V27s0bk7eHak9LqRtE68A9L4hSBTP4L4Uafz9FUwoO9uGfPLrLdNA26KYV6YzkJHQ6JmLQdMviK-hK7bKU2n8Tszjj4izVPXRfoTIzZvGqLERofoTQ011ede6vOD87UaJ8qbYvmsLh1QoaS2pCh3ZKiCHkEjsbgUTYpBPQLo3qjeEXr34DHYdlgK_ICYLoIBTtpFixETFp6jMYr3QideJbUC9vKrscQ2xbEZ4uX7v5NMuvESRRaNqrQfQ9kwPO1-x3trbZWHHdKYgzrAiYeD7vYo1YdDCc6hDTEhferKW9eS2ThYR5leeTIVmQYXvGyE1LfsO0cvsxubBIuSVKq3tVDatQScWQo34V1fdAoB9cG8aQwtjxKo9BG-UkTFiVhMuLORPSDSN3xtKjjbSgj2rABQBFbpjRzhc-aiDgAnHMDtvPfFkftFUujbi3WtifoNraVUZyKvubOrU7Y4I1GgZgzS8eF-YMpdZUDwItlqJjPA6OcdqXQbzsvg1bhOUNUrttGLSESeSUcxd_NDTX-mHGfFf9GXPT8VO83v-WmSbcYr0bw7zhnPsqxgczCcgvZFQnCYDHfrocPfQri9qhcZ_t5TRgRjOkICAcsKX_Dz1Pme8fCAA"),
                new KeyValuePair<string, string>("id_token", "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtyaU1QZG1Cdng2OHNrVDgtbVBBQjNCc2VlQSJ9.eyJhdWQiOiJjOTk0OTdhYS0zZWUyLTQ3MDctYjhhOC1jMzNmNTEzMjNmZWYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC80YWZiYzY4OS04MDViLTQ4Y2YtYTI0Yy1kNGFhMzI0OGEyNDgvIiwiaWF0IjoxNDIyMzA0MDQ5LCJuYmYiOjE0MjIzMDQwNDksImV4cCI6MTQyMjMwNzk0OSwidmVyIjoiMS4wIiwidGlkIjoiNGFmYmM2ODktODA1Yi00OGNmLWEyNGMtZDRhYTMyNDhhMjQ4IiwiYW1yIjpbInB3ZCJdLCJvaWQiOiJmODc2YWJlYi1kNmI1LTQ0ZTQtOTcxNi02MjY2YWMwMTgxYTgiLCJ1cG4iOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IlBVZGhjbFA1UGdJalNVOVAxUy1IZWxEYVNGU2YtbVhWMVk2MC1LMnZXcXciLCJnaXZlbl9uYW1lIjoiVXNlcjMiLCJmYW1pbHlfbmFtZSI6IlVzZXIzIiwibmFtZSI6IlVzZXIzIiwidW5pcXVlX25hbWUiOiJ1c2VyM0BwcmFidXJhamdtYWlsLm9ubWljcm9zb2Z0LmNvbSIsIm5vbmNlIjoiNjM1NTc5MDExNDk4NzcwNDQ5Lk1HRTBPRE01TmpNdE16ZGhaQzAwTnpObUxXRmpPREl0TWpGaU1URTVaRGsxWm1Nek1USmhabVExWkRBdFptSmtPUzAwTnpRM0xUZzRZV1V0TWpZNVlqVmlOREppTXpNNSIsImNfaGFzaCI6Im9hX2oxckRJdEhqY2tlRHBQbTA4bHciLCJwd2RfZXhwIjoiNjc4NDk5NCIsInB3ZF91cmwiOiJodHRwczovL3BvcnRhbC5taWNyb3NvZnRvbmxpbmUuY29tL0NoYW5nZVBhc3N3b3JkLmFzcHgifQ.PDVbcUPw_MXE13PTOHl1WQwoV763Lu4p-hPyc-K-UumsNwAGtQy6R5IMqNPxv86BymMdwXZjQqZPaldrjSJf7bFr9sCS_wh8IKCls4uumsRF0lC93yey5Qo7_N4NWjLw1f2QNuGcaaIimDjaoeZyGnCx84grtL-3TuSEhyGV2lc0BoovRSz_LZR4H4VnGWjVzdIZhb84LJWLjYClocWLnNdkYZAXgx4tuwAa8DckZL4JiCo1Lngpy9-ELWy8vdZqIBBwIEeO-bg9TTxxknd7kjG7OO5IKfiuAAt5121udsx9DB4TeQp5taEzFfPbOq4H3z41jlK0KCNPDDFbXU36rQ"),
                new KeyValuePair<string, string>("state", "OpenIdConnect.AuthenticationProperties=ValidStateData"),
                new KeyValuePair<string, string>("session_state", "17d814f8-618c-47a2-af6a-43df8a62279a")
            };

            response = _httpClient.PostAsync(string.Empty, new FormUrlEncodedContent(token.ToArray())).Result;
            ThrowIfResponseStatusNotOk(response);
            responseContent = response.Content.ReadAsStringAsync().Result;
        }
    }
}