// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class OpenIdConnectAuthenticateTests
{
    [Fact]
    public async Task RegularGetRequestToCallbackPathSkips()
    {
        // Arrange
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.CallbackPath = new PathString("/");
                opt.SkipUnrecognizedRequests = true;
                opt.ClientId = "Test Id";
            });

        var server = settings.CreateTestServer(handler: async context =>
        {
            await context.Response.WriteAsync("Hi from the callback path");
        });

        // Act
        var transaction = await server.SendAsync("/");

        // Assert
        Assert.Equal("Hi from the callback path", transaction.ResponseText);
    }

    [Fact]
    public async Task RegularPostRequestToCallbackPathSkips()
    {
        // Arrange
        var settings = new TestSettings(
            opt =>
            {
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.CallbackPath = new PathString("/");
                opt.SkipUnrecognizedRequests = true;
                opt.ClientId = "Test Id";
            });

        var server = settings.CreateTestServer(handler: async context =>
        {
            await context.Response.WriteAsync("Hi from the callback path");
        });

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>());

        var transaction = await server.SendAsync(request, cookieHeader: null);

        // Assert
        Assert.Equal("Hi from the callback path", transaction.ResponseText);
    }

    [Fact]
    public async Task ErrorResponseWithDetails()
    {
        var settings = new TestSettings(
            opt =>
            {
                opt.StateDataFormat = new TestStateDataFormat();
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.ClientId = "Test Id";
                opt.Events = new OpenIdConnectEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        var ex = ctx.Failure;
                        Assert.True(ex.Data.Contains("error"), "error");
                        Assert.True(ex.Data.Contains("error_description"), "error_description");
                        Assert.True(ex.Data.Contains("error_uri"), "error_uri");
                        Assert.Equal("itfailed", ex.Data["error"]);
                        Assert.Equal("whyitfailed", ex.Data["error_description"]);
                        Assert.Equal("https://example.com/fail", ex.Data["error_uri"]);
                        ctx.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(ctx.Failure.Message));
                        ctx.HandleResponse();
                        return Task.FromResult(0);
                    }
                };
            });

        var server = settings.CreateTestServer();

        var transaction = await server.SendAsync(
            "https://example.com/signin-oidc?error=itfailed&error_description=whyitfailed&error_uri=https://example.com/fail&state=protected_state",
            ".AspNetCore.Correlation.correlationId=N");
        Assert.Equal(HttpStatusCode.Redirect, transaction.Response.StatusCode);
        Assert.StartsWith("/error?FailureMessage=", transaction.Response.Headers.GetValues("Location").First());
    }

    [Fact]
    // Regression test for https://github.com/dotnet/aspnetcore/issues/53048. An error response
    // (for example prompt=none returning error=login_required) must still delete the nonce and
    // correlation cookies for that flow, otherwise they accumulate on repeated failed sign-ins
    // until the request headers grow too large. The test runs a real challenge and replays its
    // state and cookies on the callback, so it covers the nonce the handler records at challenge
    // time as well as the cleanup.
    public async Task ErrorResponseDeletesNonceAndCorrelationCookies()
    {
        AuthenticationProperties failureProperties = null;
        var settings = new TestSettings(
            opt =>
            {
                opt.StateDataFormat = new RoundTripStateDataFormat();
                opt.Authority = TestServerBuilder.DefaultAuthority;
                opt.ClientId = "Test Id";
                opt.Events = new OpenIdConnectEvents()
                {
                    OnRemoteFailure = ctx =>
                    {
                        failureProperties = ctx.Properties;
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                        return Task.CompletedTask;
                    }
                };
            });

        // An application item must round-trip untouched next to the nonce the handler stores.
        var challengeProperties = new AuthenticationProperties();
        challengeProperties.Items["N"] = "app-value";
        var server = settings.CreateTestServer(challengeProperties);

        var challenge = await server.SendAsync(TestServerBuilder.TestHost + TestServerBuilder.ChallengeWithProperties);
        var challengeCookies = SetCookieHeaderValue.ParseList(challenge.SetCookie);
        var nonceCookie = challengeCookies.Single(cookie => cookie.Name.StartsWith(OpenIdConnectDefaults.CookieNoncePrefix, StringComparison.Ordinal));
        var correlationCookie = challengeCookies.Single(cookie => cookie.Name.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal));

        var transaction = await server.SendAsync(
            "https://example.com/signin-oidc?error=login_required&state=protected_state",
            $"{nonceCookie.Name}={nonceCookie.Value}; {correlationCookie.Name}={correlationCookie.Value}");

        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        Assert.Contains(
            transaction.SetCookie,
            cookie => cookie.StartsWith($"{nonceCookie.Name}=; expires=Thu, 01 Jan 1970", StringComparison.Ordinal));
        Assert.Contains(
            transaction.SetCookie,
            cookie => cookie.StartsWith($"{correlationCookie.Name}=; expires=Thu, 01 Jan 1970", StringComparison.Ordinal));
        Assert.NotNull(failureProperties);
        Assert.Equal("app-value", failureProperties.Items["N"]);
    }

    private class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private AuthenticationProperties Data { get; set; }

        public string Protect(AuthenticationProperties data)
        {
            return "protected_state";
        }

        public string Protect(AuthenticationProperties data, string purpose)
        {
            throw new NotImplementedException();
        }

        public AuthenticationProperties Unprotect(string protectedText)
        {
            Assert.Equal("protected_state", protectedText);
            var properties = new AuthenticationProperties(new Dictionary<string, string>()
                {
                    { ".xsrf", "correlationId" },
                    { "testkey", "testvalue" }
                });
            properties.RedirectUri = "http://testhost/redirect";
            return properties;
        }

        public AuthenticationProperties Unprotect(string protectedText, string purpose)
        {
            throw new NotImplementedException();
        }
    }

    // Hands the state produced at challenge time back on the callback, standing in for real
    // protection so the test sees exactly what the handler stored.
    private class RoundTripStateDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private Dictionary<string, string> _items;

        public string Protect(AuthenticationProperties data)
        {
            _items = new Dictionary<string, string>(data.Items);
            return "protected_state";
        }

        public string Protect(AuthenticationProperties data, string purpose) => throw new NotImplementedException();

        public AuthenticationProperties Unprotect(string protectedText)
        {
            Assert.Equal("protected_state", protectedText);
            return new AuthenticationProperties(new Dictionary<string, string>(_items));
        }

        public AuthenticationProperties Unprotect(string protectedText, string purpose) => throw new NotImplementedException();
    }
}
