// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Tests;

public class CsrfProtectionIntegrationTests
{
    private const string CsrfProtectionInvokedKey = "__CsrfProtectionMiddlewareWithEndpointInvoked";

    [Fact]
    public async Task CsrfProtection_AutoInjected_BlocksCrossOriginPost()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_AutoInjected_AllowsSameOriginPost()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_AutoInjected_AllowsGetRequests()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/protected-get");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_AutoInjected_AllowsNonBrowserClients()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        // No Sec-Fetch-Site, no Origin → non-browser client
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_DisabledEndpoint_AllowsCrossOrigin()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/unprotected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_DisabledViaUseSetting_AllowsCrossOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.WebHost.UseSetting("DisableCsrfProtection", "true");
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public async Task CsrfProtection_DisableCsrfProtectionConfig_AcceptedValues(string value)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.WebHost.UseSetting("DisableCsrfProtection", value);
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("disable")]
    [InlineData("yes")]
    [InlineData("")]
    public async Task CsrfProtection_DisableCsrfProtectionConfig_RejectedValues_StillProtects(string value)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.WebHost.UseSetting("DisableCsrfProtection", value);
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_NotRegistered_AllowsCrossOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        // Remove the auto-registered ICsrfProtection to simulate disabled service
        builder.Services.RemoveAll<ICsrfProtection>();
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_CustomImplementation_IsUsed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICsrfProtection, AlwaysAllowCsrfProtection>();
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_SecFetchSiteNone_Allowed()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "none");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_SecFetchSiteSameSite_Denied()
    {
        using var app = await CreateApp();
        var client = app.GetTestClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "same-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_TrustedOriginViaCorsDefaultPolicy_AllowsCrossOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.WithOrigins("https://trusted.example.com")));
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Origin", "https://trusted.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_CorsDefaultPolicyAllowAnyOrigin_IsIgnored()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin()));
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_UntrustedOrigin_Denied()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.WithOrigins("https://trusted.example.com")));
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_PerEndpointEnableCors_TrustsNamedPolicyOrigins()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com")));
        using var app = builder.Build();

        app.UseCors();
        app.MapPost("/webhook", EnforceCsrf).RequireCors("Webhook");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://stripe.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_PerEndpointEnableCors_OverridesDefaultPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.WithOrigins("https://app.example.com"));
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com"));
        });
        using var app = builder.Build();

        app.UseCors();
        app.MapPost("/webhook", EnforceCsrf).RequireCors("Webhook");
        await app.StartAsync();

        var client = app.GetTestClient();
        // Default-policy origin would be allowed app-wide, but this endpoint declared a stricter named policy.
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://app.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_PerEndpointInlineCorsPolicy_TrustsConfiguredOrigins()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors();
        using var app = builder.Build();

        app.UseCors();
        // Inline-policy variant: RequireCors(lambda) builds a CorsPolicy and attaches it as ICorsPolicyMetadata.
        app.MapPost("/webhook", EnforceCsrf).RequireCors(p => p.WithOrigins("https://stripe.example.com"));
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://stripe.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_NoDefaultPolicy_NamedPolicyOnly_UnannotatedEndpoint_FallsThrough()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        // AddCors with only a named policy, no AddDefaultPolicy. Endpoint has no [EnableCors] either.
        builder.Services.AddCors(options =>
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com")));
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        // The named policy's origin is NOT trusted here because the endpoint never opted into it
        // and there's no default policy to fall back to → cross-site Sec-Fetch denies.
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://stripe.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_PerEndpointDisableCors_FallsThroughToSecFetchSite()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy => policy.WithOrigins("https://trusted.example.com")));
        using var app = builder.Build();

        // [DisableCors] tells us this endpoint has no CORS-derived trust list.
        // The default-policy origin would have been trusted app-wide, but here it isn't.
        // UseCors is required because the endpoint carries CORS metadata; the CSRF middleware no longer
        // short-circuits, so the request now reaches the endpoint pipeline.
        app.UseCors();
        app.MapPost("/no-cors", EnforceCsrf).WithMetadata(new Cors.DisableCorsAttribute());
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/no-cors");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://trusted.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_ExplicitUseRouting_NamedPolicy_TrustsAllowedOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com")));
        using var app = builder.Build();

        app.UseRouting();
        app.UseCors();
        app.MapPost("/webhook", EnforceCsrf).RequireCors("Webhook");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://stripe.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_AutoRouting_NamedPolicy_TrustsAllowedOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com")));
        using var app = builder.Build();

        app.UseCors();
        app.MapPost("/webhook", EnforceCsrf).RequireCors("Webhook");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://stripe.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_ExplicitUseRouting_NamedPolicy_DeniesUntrustedOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
            options.AddPolicy("Webhook", policy => policy.WithOrigins("https://stripe.example.com")));
        using var app = builder.Build();

        app.UseRouting();
        app.UseCors();
        app.MapPost("/webhook", EnforceCsrf).RequireCors("Webhook");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostRoutingPipeline_ApplicationProvidedClosure_IsRejected()
    {
        // '__Internal_PostRoutingPipeline' is a framework-reserved slot used to run the implicit authentication,
        // authorization and CSRF middleware immediately after routing matches. IApplicationBuilder.Properties is
        // publicly writable, so EndpointRoutingMiddleware must reject any delegate that application code places there
        // instead of executing it in the matched endpoint's scope. A closure has a compiler-generated method name,
        // which fails the framework method-name check.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        var tampered = false;
        app.Properties["__Internal_PostRoutingPipeline"] = (Func<RequestDelegate, RequestDelegate>)(next => context =>
        {
            tampered = true;
            return next(context);
        });
        app.MapGet("/", () => "hello");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Contains("__Internal_PostRoutingPipeline", exception.Message);
        Assert.False(tampered);
    }

    [Fact]
    public async Task PostRoutingPipeline_DelegateFromAnotherAssembly_IsRejected()
    {
        // Even a delegate whose method is named "CreateMiddleware" (matching the framework holder) is rejected when it
        // is declared in an assembly other than Microsoft.AspNetCore. This proves the trust check does not rely on the
        // method name alone and can't be satisfied by application code mimicking the framework's shape.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        var impostor = new ImpostorPostRoutingPipeline();
        app.Properties["__Internal_PostRoutingPipeline"] = (Func<RequestDelegate, RequestDelegate>)impostor.CreateMiddleware;
        app.MapGet("/", () => "hello");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Contains("__Internal_PostRoutingPipeline", exception.Message);
        Assert.False(impostor.Invoked);
    }

    [Fact]
    public async Task PostRoutingPipeline_NonDelegateValue_IsRejected()
    {
        // A non-delegate value in the reserved slot is also rejected rather than silently ignored.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        app.Properties["__Internal_PostRoutingPipeline"] = "not a delegate";
        app.MapGet("/", () => "hello");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync());
        Assert.Contains("__Internal_PostRoutingPipeline", exception.Message);
    }

    [Fact]
    public async Task PostRoutingPipeline_FrameworkBlock_RunsImplicitMiddlewareAfterRouting()
    {
        // The trust check must not reject the framework's own block. With an explicit UseRouting(), the deferred
        // authentication/authorization/CSRF block is created by the framework and accepted, so a same-origin POST to a
        // protected endpoint succeeds (CSRF runs after routing and sees the matched endpoint).
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        app.UseRouting();
        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Origin", "https://localhost");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // A type that mimics the framework holder's shape (a public instance method named CreateMiddleware returning a
    // RequestDelegate) but lives in the test assembly, so the framework trust check must reject it.
    private sealed class ImpostorPostRoutingPipeline
    {
        public bool Invoked { get; private set; }

        public RequestDelegate CreateMiddleware(RequestDelegate next)
        {
            Invoked = true;
            return next;
        }
    }

    // The CSRF middleware does not short-circuit; it records its verdict on IAntiforgeryValidationFeature and lets downstream consumers decide.
    // This endpoint mirrors how a real consumer (the MVC antiforgery filter, minimal-API form binding, Razor Components) reacts to that verdict: reject when IsValid is false.
    private static string EnforceCsrf(HttpContext context)
    {
        if (context.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false })
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return "denied";
        }

        return "ok";
    }

    private static async Task<WebApplication> CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        app.MapGet("/protected-get", EnforceCsrf);
        app.MapPost("/unprotected", EnforceCsrf).DisableAntiforgery();

        await app.StartAsync();
        return app;
    }

    [Fact]
    public async Task CsrfProtection_CustomImplementation_IsResolvedFromDIAndInvokedPerRequest()
    {
        // Verifies that a custom ICsrfProtection registered before Build() is the exact instance
        // resolved by the auto-injected middleware, and that its ValidateAsync is invoked once per request.
        var counting = new CountingCsrfProtection();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ICsrfProtection>(counting);
        using var app = builder.Build();

        // Sanity: the instance the container returns is the one we registered.
        Assert.Same(counting, app.Services.GetRequiredService<ICsrfProtection>());

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        for (var i = 0; i < 3; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
            request.Headers.Add("Sec-Fetch-Site", "cross-site");
            var response = await client.SendAsync(request);
            // Custom implementation always allows, so cross-site POST gets through.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal(3, counting.CallCount);
    }

    [Fact]
    public async Task CsrfProtection_FailedValidation_LogsDebug()
    {
        var captureProvider = new CaptureLoggerProvider("Microsoft.AspNetCore.Antiforgery.CsrfProtectionMiddleware", LogLevel.Debug);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddProvider(captureProvider).SetMinimumLevel(LogLevel.Debug);
        using var app = builder.Build();

        app.MapPost("/protected", EnforceCsrf);
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var entry = Assert.Single(captureProvider.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("marked request POST /protected", entry.Message);
        Assert.Contains("https://evil.example.com", entry.Message);
    }

    [Fact]
    public async Task CsrfProtection_AllowedRequest_ReadFormUrlEncoded_Succeeds()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        string? capturedName = null;
        string? capturedColor = null;
        app.MapPost("/form", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            capturedName = form["name"];
            capturedColor = form["color"];
            return "ok";
        });
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/form")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("name", "alice"),
                new KeyValuePair<string, string>("color", "purple"),
            ]),
        };
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alice", capturedName);
        Assert.Equal("purple", capturedColor);
    }

    [Fact]
    public async Task CsrfProtection_AllowedRequest_ReadFormMultipart_Succeeds()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        string? capturedName = null;
        string? capturedFileContent = null;
        app.MapPost("/form", async (HttpContext context) =>
        {
            var form = await context.Request.ReadFormAsync();
            capturedName = form["name"];
            var file = form.Files["upload"];
            if (file is not null)
            {
                using var reader = new StreamReader(file.OpenReadStream());
                capturedFileContent = await reader.ReadToEndAsync();
            }
            return "ok";
        });
        await app.StartAsync();

        var client = app.GetTestClient();
        var multipart = new MultipartFormDataContent
        {
            { new StringContent("alice"), "name" },
            { new StringContent("hello world"), "upload", "greeting.txt" },
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/form") { Content = multipart };
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alice", capturedName);
        Assert.Equal("hello world", capturedFileContent);
    }

    [Fact]
    public async Task CsrfProtection_DeniedRequest_FormBindingEndpoint_RejectsBeforeHandler()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        // A minimal-API endpoint that binds the form. The CSRF does not short-circuit;
        // instead it records IsValid=false and the form-binding consumer (RequestDelegateFactory)
        // rejects the request with 400 before the handler runs.
        var endpointInvoked = false;
        app.MapPost("/form", (IFormCollection form) =>
        {
            endpointInvoked = true;
            return "ok";
        });
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/form")
        {
            Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("name", "alice") }),
        };
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(endpointInvoked, "Form binding should reject the recorded CSRF failure before reaching the endpoint.");
    }

    [Fact]
    public async Task CsrfProtection_NonFormEndpoint_CrossOrigin_IsNotAutoRejected_ButRecordsInvalidVerdict()
    {
        // Behavior change: like token-based antiforgery, the CSRF middleware does not reject plain
        // (non-form) endpoints on its own. It records the verdict; a consumer that never reads the
        // feature simply runs. This documents that a bare MapPost is reachable cross-origin.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        IAntiforgeryValidationFeature? capturedFeature = null;
        app.MapPost("/plain", (HttpContext ctx) =>
        {
            capturedFeature = ctx.Features.Get<IAntiforgeryValidationFeature>();
            return "ok";
        });
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/plain");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedFeature);
        Assert.False(capturedFeature!.IsValid, "CSRF middleware should still record the cross-origin denial on the feature.");
        Assert.NotNull(capturedFeature.Error);
    }

    [Fact]
    public async Task CsrfProtection_AndAntiforgeryMiddleware_FormReadRejectsMissingToken()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery();
        using var app = builder.Build();

        app.UseAntiforgery();
        IAntiforgeryValidationFeature? capturedFeature = null;
        app.MapPost("/protected", (HttpContext ctx) =>
        {
            capturedFeature = ctx.Features.Get<IAntiforgeryValidationFeature>();
            return "ok";
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        await app.StartAsync();

        var client = app.GetTestClient();
        // Same-origin POST with no antiforgery token. CSRF middleware allows (Sec-Fetch-Site=same-origin)
        // but antiforgery middleware must still record the missing token on IAntiforgeryValidationFeature.
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected")
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("name", "alice")]),
        };
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedFeature);
        Assert.False(capturedFeature!.IsValid, "Antiforgery middleware should record the missing token even when CSRF protection allowed the request.");
    }

    [Fact]
    public async Task CsrfProtection_AndAntiforgeryMiddleware_ValidTokenSucceeds()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery();
        using var app = builder.Build();

        app.UseAntiforgery();
        IAntiforgeryValidationFeature? capturedFeature = null;
        app.MapPost("/protected", (HttpContext ctx) =>
        {
            capturedFeature = ctx.Features.Get<IAntiforgeryValidationFeature>();
            return "ok";
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());

        await app.StartAsync();

        var client = app.GetTestClient();
        // Generate a valid token pair (cookie + form field) via the configured IAntiforgery service.
        var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AntiforgeryOptions>>().Value;
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext { RequestServices = app.Services });

        var request = new HttpRequestMessage(HttpMethod.Post, "/protected")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>(options.FormFieldName, tokens.RequestToken!),
            }),
        };
        request.Headers.Add("Sec-Fetch-Site", "same-origin");
        request.Headers.Add("Cookie", $"{options.Cookie.Name}={tokens.CookieToken}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(capturedFeature);
        Assert.True(capturedFeature!.IsValid, "Antiforgery middleware should accept the valid token; CSRF protection also allowed the same-origin request.");
    }

    [Theory]
    [InlineData("same-origin", /* withToken */ true)]   // same-origin + valid token → valid
    [InlineData("same-origin", /* withToken */ false)]  // same-origin allowed by CSRF, but AF records the missing token → invalid
    [InlineData("cross-site",  /* withToken */ true)]   // cross-site denied by CSRF, but a valid token makes AF override the verdict → valid
    [InlineData("cross-site",  /* withToken */ false)]  // cross-site denied by CSRF and AF also fails → invalid
    public async Task CsrfProtection_BlazorShapedEndpoint_AntiforgeryTokenResultOverridesCsrfVerdict(string secFetchSite, bool withToken)
    {
        // Blazor server-rendered components register every page endpoint with
        // `RequireAntiforgeryTokenAttribute` metadata (see `RazorComponentEndpointFactory`)
        // This test simulates that shape without pulling in the Blazor packages.
        //
        // The CSRF middleware records its Sec-Fetch verdict early, then the token-based
        // AntiforgeryMiddleware (at the UseAntiforgery() position) runs and overwrites the verdict
        // with the token-validation result. Neither middleware short-circuits, so the manual handler
        // always runs; the recorded IsValid reflects the token outcome regardless of Sec-Fetch-Site.

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAntiforgery();
        using var app = builder.Build();

        app.UseAntiforgery();

        var endpointInvoked = false;
        IAntiforgeryValidationFeature? capturedFeature = null;
        app.MapPost("/page", (HttpContext ctx) =>
        {
            endpointInvoked = true;
            capturedFeature = ctx.Features.Get<IAntiforgeryValidationFeature>();
            return "ok";
        }).WithMetadata(new RequireAntiforgeryTokenAttribute()); // Same metadata Blazor adds per page

        await app.StartAsync();

        var client = app.GetTestClient();
        var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AntiforgeryOptions>>().Value;
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext { RequestServices = app.Services });

        var formFields = new List<KeyValuePair<string, string>>
        {
            new("name", "alice"),
        };
        if (withToken)
        {
            formFields.Add(new KeyValuePair<string, string>(options.FormFieldName, tokens.RequestToken!));
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "/page")
        {
            Content = new FormUrlEncodedContent(formFields),
        };
        request.Headers.Add("Sec-Fetch-Site", secFetchSite);
        if (withToken)
        {
            request.Headers.Add("Cookie", $"{options.Cookie.Name}={tokens.CookieToken}");
        }

        var response = await client.SendAsync(request);

        // The manual handler isn't a form-binding consumer, so nothing rejects on its behalf: it always runs.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(endpointInvoked);

        // The recorded verdict is the AntiforgeryMiddleware token result, overriding the CSRF Sec-Fetch verdict.
        Assert.NotNull(capturedFeature);
        Assert.Equal(withToken, capturedFeature!.IsValid);
    }

    [Fact]
    public async Task CsrfProtection_SetsItemsMarker_WhenEndpointMatched()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        object? observedMarker = null;
        app.MapPost("/probe", (HttpContext ctx) =>
        {
            observedMarker = ctx.Items[CsrfProtectionInvokedKey];
            return "ok";
        });

        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/probe");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(observedMarker);
    }

    [Fact]
    public async Task CsrfProtection_SetsItemsMarker_EvenWhenEndpointDisabledAntiforgery()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();

        object? observedMarker = null;
        app.MapPost("/exempt", (HttpContext ctx) =>
        {
            observedMarker = ctx.Items[CsrfProtectionInvokedKey];
            return "ok";
        }).DisableAntiforgery();

        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/exempt");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(observedMarker);
    }

    private sealed class AlwaysAllowCsrfProtection : ICsrfProtection
    {
        public ValueTask<CsrfProtectionResult> ValidateAsync(HttpContext context)
            => new(CsrfProtectionResult.Allowed());
    }

    private sealed class CountingCsrfProtection : ICsrfProtection
    {
        public int CallCount { get; private set; }

        public ValueTask<CsrfProtectionResult> ValidateAsync(HttpContext context)
        {
            CallCount++;
            return new ValueTask<CsrfProtectionResult>(CsrfProtectionResult.Allowed());
        }
    }

    private sealed class CaptureLoggerProvider : ILoggerProvider
    {
        private readonly string _categoryFilter;
        private readonly LogLevel _minLevel;

        public List<LogEntry> Entries { get; } = new();

        public CaptureLoggerProvider(string categoryFilter, LogLevel minLevel)
        {
            _categoryFilter = categoryFilter;
            _minLevel = minLevel;
        }

        public ILogger CreateLogger(string categoryName)
            => string.Equals(categoryName, _categoryFilter, StringComparison.Ordinal)
                ? new CaptureLogger(this, _minLevel)
                : NullLogger.Instance;

        public void Dispose() { }

        internal sealed record LogEntry(LogLevel Level, EventId EventId, string Message);

        private sealed class CaptureLogger : ILogger
        {
            private readonly CaptureLoggerProvider _provider;
            private readonly LogLevel _minLevel;

            public CaptureLogger(CaptureLoggerProvider provider, LogLevel minLevel)
            {
                _provider = provider;
                _minLevel = minLevel;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                lock (_provider.Entries)
                {
                    _provider.Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
                }
            }
        }
    }
}
