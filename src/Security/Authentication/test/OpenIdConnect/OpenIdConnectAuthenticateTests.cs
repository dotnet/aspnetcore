// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

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
}
