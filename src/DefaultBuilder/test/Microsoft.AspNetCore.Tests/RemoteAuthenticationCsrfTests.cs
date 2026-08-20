// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Tests;

// A remote provider's callback (e.g. OpenID Connect response_mode=form_post) is a cross-site form POST by
// protocol design. When the callback path also matches a routed endpoint that requires antiforgery
// validation, the auto-injected CSRF middleware records an invalid verdict for it, and the handler used to
// fail while reading its own callback body - before any of its events could run.
public class RemoteAuthenticationCsrfTests
{
    [Fact]
    public async Task RemoteCallback_CrossSiteFormPost_CanReadCallbackForm()
    {
        using var app = await CreateAppWithTokenAntiforgery();

        var response = await app.GetTestClient().SendAsync(CreateCallbackRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("handled:2", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RemoteCallback_WhenHandlerSkipsRequest_WithTokenAntiforgery_ProtectsDownstreamEndpoint()
    {
        using var app = await CreateAppWithTokenAntiforgery(skipRequest: true);

        var response = await app.GetTestClient().SendAsync(CreateCallbackRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("protected", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RemoteCallback_WhenHandlerSkipsRequest_RestoresAutoCsrfVerdict()
    {
        using var app = await CreateAppWithAutoCsrfOnly(skipRequest: true);

        var response = await app.GetTestClient().SendAsync(CreateCallbackRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("protected", await response.Content.ReadAsStringAsync());
    }

    private static HttpRequestMessage CreateCallbackRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/signin-oidc")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["state"] = "fakestate",
                ["code"] = "fakecode",
            })
        };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        return request;
    }

    private static Task<WebApplication> CreateAppWithTokenAntiforgery(bool skipRequest = false)
        => CreateApp(skipRequest, useTokenAntiforgery: true);

    private static Task<WebApplication> CreateAppWithAutoCsrfOnly(bool skipRequest = false)
        => CreateApp(skipRequest, useTokenAntiforgery: false);

    private static async Task<WebApplication> CreateApp(bool skipRequest, bool useTokenAntiforgery)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication("signin")
            .AddScheme<AuthenticationSchemeOptions, NoOpHandler>("signin", _ => { })
            .AddScheme<FakeRemoteOptions, FakeRemoteHandler>("remote", o =>
            {
                o.CallbackPath = "/signin-oidc";
                o.SignInScheme = "signin";
                o.SkipRequest = skipRequest;
            });
        builder.Services.AddAuthorization();
        if (useTokenAntiforgery)
        {
            builder.Services.AddAntiforgery();
        }

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        if (useTokenAntiforgery)
        {
            app.UseAntiforgery();
        }

        // Stands in for a catch-all server-rendered page: it makes routing match the remote callback path,
        // which is what causes the CSRF middleware to record a verdict for the callback request.
        app.MapPost("/{**slug}", EnforceCsrf).WithMetadata(new RequiresValidationMetadata());

        await app.StartAsync();
        return app;
    }

    private static string EnforceCsrf(HttpContext context)
    {
        var feature = context.Features.Get<IAntiforgeryValidationFeature>();
        if (feature is null)
        {
            return "passthrough";
        }

        if (!feature.IsValid)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return "protected";
        }

        return "allowed";
    }

    private sealed class RequiresValidationMetadata : IAntiforgeryMetadata
    {
        public bool RequiresValidation => true;
    }

    private sealed class NoOpHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());
    }

    private sealed class FakeRemoteOptions : RemoteAuthenticationOptions
    {
        public bool SkipRequest { get; set; }
    }

    private sealed class FakeRemoteHandler(IOptionsMonitor<FakeRemoteOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : RemoteAuthenticationHandler<FakeRemoteOptions>(options, logger, encoder)
    {
        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            // Mirrors OpenIdConnectHandler.HandleRemoteAuthenticateAsync: the form_post callback body is read
            // before the handler raises any of its events.
            var form = await Request.ReadFormAsync(Context.RequestAborted);

            if (Options.SkipRequest)
            {
                return HandleRequestResult.SkipHandler();
            }

            Response.StatusCode = StatusCodes.Status200OK;
            await Response.WriteAsync($"handled:{form.Count}");
            return HandleRequestResult.Handle();
        }
    }
}
