// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using Xunit;
using System.Net.Http;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    public class TraditionalWebApplicationTests
    {
        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "https://github.com/aspnet/Identity/issues/1346")]
        public async Task CanPerform_AuthorizationCode_Flow()
        {
            // Arrange   
            var clientId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

            var appBuilder = new CredentialsServerBuilder()
                .EnsureDeveloperCertificate()
                .ConfigureReferenceData(data => data
                    .CreateIntegratedWebClientApplication(clientId)
                    .CreateResourceApplication(resourceId, "ResourceApplication", "read")
                    .CreateUser("testUser", "Pa$$w0rd"))
                .ConfigureInMemoryEntityFrameworkStorage()
                .ConfigureMvcAutomaticSignIn()
                .ConfigureOpenIdConnectClient(options =>
                {
                    options.ClientId = clientId;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.ResponseMode = OpenIdConnectResponseMode.Query;
                    options.Scope.Add("https://localhost/DFC7191F-FF74-42B9-A292-08FEA80F5B20/v2.0/ResourceApplication/read");
                })
                .ConfigureIntegratedClient(clientId);

            var client = appBuilder.Build();

            // Act & Assert

            // Navigate to protected resource.
            var goToAuthorizeResponse = await client.GetAsync("https://localhost/Home/About");

            // Redirected to authorize
            var location = ResponseAssert.IsRedirect(goToAuthorizeResponse);
            var oidcCookiesComparisonCriteria = CookieComparison.Strict & ~CookieComparison.NameEquals | CookieComparison.NameStartsWith;
            ResponseAssert.HasCookie(CreateExpectedSetNonceCookie(), goToAuthorizeResponse, oidcCookiesComparisonCriteria);
            ResponseAssert.HasCookie(CreateExpectedSetCorrelationIdCookie(), goToAuthorizeResponse, oidcCookiesComparisonCriteria);
            var authorizeParameters = ResponseAssert.LocationHasQueryParameters<OpenIdConnectMessage>(
                goToAuthorizeResponse,
                "state");

            // Navigate to authorize
            var goToLoginResponse = await client.GetAsync(location);

            // Redirected to login
            location = ResponseAssert.IsRedirect(goToLoginResponse);

            // Navigate to login
            var goToAuthorizeWithCookie = await client.GetAsync(location);

            // Stamp a login cookie and redirect back to authorize.
            location = ResponseAssert.IsRedirect(goToAuthorizeWithCookie);
            ResponseAssert.HasCookie(".AspNetCore.Identity.Application", goToAuthorizeWithCookie, CookieComparison.NameEquals);

            // Navigate to authorize with a login cookie.
            var goToSignInOidcCallback = await client.GetAsync(location);

            // Stamp an application session cookie and redirect to relying party callback with an authorization code on the query string.
            location = ResponseAssert.IsRedirect(goToSignInOidcCallback);
            ResponseAssert.HasCookie("Microsoft.AspNetCore.Identity.Service", goToSignInOidcCallback, CookieComparison.NameEquals);
            var callBackQueryParameters = ResponseAssert.LocationHasQueryParameters<OpenIdConnectMessage>(goToSignInOidcCallback, "code", ("state", authorizeParameters.State));

            // Navigate to relying party callback.
            var goToProtectedResource = await client.GetAsync(location);

            // Stamp a session cookie and redirect to the protected resource.
            location = ResponseAssert.IsRedirect(goToProtectedResource);
            ResponseAssert.HasCookie(".AspNetCore.Cookies", goToProtectedResource, CookieComparison.NameEquals);
            ResponseAssert.HasCookie(CreateExpectedSetCorrelationIdCookie(DateTime.Parse("1/1/1970 12:00:00 AM +00:00")), goToProtectedResource, CookieComparison.Delete);
            ResponseAssert.HasCookie(CreateExpectedSetNonceCookie(DateTime.Parse("1/1/1970 12:00:00 AM +00:00")), goToProtectedResource, CookieComparison.Delete);

            var protectedResourceResponse = await client.GetAsync(location);
            ResponseAssert.IsOK(protectedResourceResponse);
            ResponseAssert.IsHtmlDocument(protectedResourceResponse);
        }

        private SetCookieHeaderValue CreateExpectedSetCorrelationIdCookie(DateTime expires = default(DateTime))
        {
            return new SetCookieHeaderValue(new StringSegment(".AspNetCore.Correlation.OpenIdConnect."), new StringSegment("N"))
            {
                Expires = expires == default(DateTime) ? DateTime.UtcNow.AddMinutes(15) : expires,
                Path = "/",
                Secure = true,
                HttpOnly = true
            };
        }

        private static SetCookieHeaderValue CreateExpectedSetNonceCookie(DateTime expires = default(DateTime))
        {
            return new SetCookieHeaderValue(new StringSegment(".AspNetCore.OpenIdConnect.Nonce."), new StringSegment("N"))
            {
                Expires = expires == default(DateTime) ? DateTime.UtcNow.AddMinutes(15) : expires,
                Path = "/",
                Secure = true,
                HttpOnly = true
            };
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR, SkipReason = "https://github.com/aspnet/Identity/issues/1346")]
        public async Task CanPerform_IdToken_Flow()
        {
            // Arrange          
            var clientId = Guid.NewGuid().ToString();
            var resourceId = Guid.NewGuid().ToString();

            var appBuilder = new CredentialsServerBuilder()
                .EnsureDeveloperCertificate()
                .ConfigureReferenceData(data => data
                    .CreateIntegratedWebClientApplication(clientId)
                    .CreateUser("testUser", "Pa$$w0rd"))
                .ConfigureInMemoryEntityFrameworkStorage()
                .ConfigureMvcAutomaticSignIn()
                .ConfigureOpenIdConnectClient(options =>
                {
                    options.ClientId = clientId;
                })
                .ConfigureIntegratedClient(clientId);

            var client = appBuilder.Build();

            // Act
            var goToAuthorizeResponse = await client.GetAsync("https://localhost/Home/About");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, goToAuthorizeResponse.StatusCode);

            // Act
            var goToLoginResponse = await client.GetAsync(goToAuthorizeResponse.Headers.Location);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, goToLoginResponse.StatusCode);

            // Act
            var goToAuthorizeWithCookie = await client.GetAsync(goToLoginResponse.Headers.Location);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, goToAuthorizeWithCookie.StatusCode);

            // Act
            var goToSignInOidcCallback = await client.GetAsync(goToAuthorizeWithCookie.Headers.Location);

            // Assert
            Assert.Equal(HttpStatusCode.OK, goToSignInOidcCallback.StatusCode);
            ResponseAssert.IsHtmlDocument(goToSignInOidcCallback);
            var form = GetForm(await goToSignInOidcCallback.Content.ReadAsStringAsync());
            var formRequest = new HttpRequestMessage(new HttpMethod(form.Method), form.Action)
            {
                Content = new FormUrlEncodedContent(form.Values)
            };

            // Act
            var goToProtectedResource = await client.SendAsync(formRequest);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, goToProtectedResource.StatusCode);

            // Act
            var protectedResourceResponse = await client.GetAsync(goToProtectedResource.Headers.Location);

            // Assert
            Assert.Equal(HttpStatusCode.OK, protectedResourceResponse.StatusCode);
            ResponseAssert.IsHtmlDocument(protectedResourceResponse);
        }


        public class Form
        {
            public string Action { get; set; }
            public string Method { get; set; }
            public IList<KeyValuePair<string, string>> Values { get; set; } = new List<KeyValuePair<string, string>>();
        }

        private Form GetForm(string html)
        {
            var formHeader = Regex.Match(html, @"<form name=""form"" method=""post"" action=""(?<action>[^""]+)"">");
            var formValues = Regex.Matches(html, @"<input type=""hidden"" name=""(?<name>[^""]+)"" value=""(?<value>[^""]+)"" />",RegexOptions.Multiline)
                .OfType<Match>()
                .ToList();

            return new Form
            {
                Method = "POST",
                Action = formHeader.Groups["action"].Captures[0].Value,
                Values = formValues.Select(m => new KeyValuePair<string, string>(m.Groups["name"].Captures[0].Value, m.Groups["value"].Captures[0].Value)).ToList()
            };
        }
    }
}
