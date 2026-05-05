// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

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

namespace Microsoft.AspNetCore.Tests;

public class CsrfProtectionIntegrationTests
{
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
        builder.WebHost.UseSetting("CrossOriginProtection", "disable");
        using var app = builder.Build();

        app.MapPost("/protected", () => "ok");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_NotRegistered_AllowsCrossOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        // Remove the auto-registered ICsrfProtection to simulate disabled service
        builder.Services.RemoveAll<ICsrfProtection>();
        using var app = builder.Build();

        app.MapPost("/protected", () => "ok");
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

        app.MapPost("/protected", () => "ok");
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
    public async Task CsrfProtection_CorsTrustedOrigin_AllowsCrossOrigin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins("https://trusted.example.com"));
        });
        using var app = builder.Build();

        app.MapPost("/protected", () => "ok");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Origin", "https://trusted.example.com");
        // No Sec-Fetch-Site → falls to Origin check → matches trusted origins

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_CorsUntrustedOrigin_DeniedWithSecFetch()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins("https://trusted.example.com"));
        });
        using var app = builder.Build();

        app.MapPost("/protected", () => "ok");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CsrfProtection_CorsAllowAnyOrigin_NotUsedAsTrusted()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy.AllowAnyOrigin());
        });
        using var app = builder.Build();

        app.MapPost("/protected", () => "ok");
        await app.StartAsync();

        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/protected");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<WebApplication> CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.MapPost("/protected", () => "ok");
        app.MapGet("/protected-get", () => "ok");
        app.MapPost("/unprotected", () => "ok").DisableAntiforgery();

        await app.StartAsync();
        return app;
    }

    private sealed class AlwaysAllowCsrfProtection : ICsrfProtection
    {
        public CsrfProtectionResult Validate(HttpContext context) => CsrfProtectionResult.Allowed;
    }
}
