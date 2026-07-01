// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Gateway;

public class BlazorGatewayTests
{
    [Fact]
    public async Task HealthChecks_ReturnsOk_InDevelopment_WithDefaultOptions()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development);

        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/alive")).StatusCode);
    }

    [Fact]
    public async Task HealthChecks_Liveness_AlwaysMapped_Health_OnlyInDevelopment()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production);

        // /alive is mapped in all environments so external orchestrators can probe
        // a running deployment; the broader /health aggregate is dev-only because
        // exposing it publicly leaks dependency status.
        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task HealthChecks_Disabled_ReturnsNotFound_EvenInDevelopment()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development, new()
        {
            ["Gateway:HealthChecks:Enabled"] = "false",
        });

        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/alive")).StatusCode);
    }

    [Fact]
    public async Task HealthChecks_HonorsCustomPaths()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development, new()
        {
            ["Gateway:HealthChecks:Path"] = "/healthz",
            ["Gateway:HealthChecks:LivenessPath"] = "/livez",
        });

        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/healthz")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/livez")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/alive")).StatusCode);
    }

    [Fact]
    public async Task HealthChecks_Liveness_FlipsToUnhealthy_DuringShutdown()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development);

        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/health")).StatusCode);

        var lifetime = gateway.App.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.StopApplication();

        // The shutdown-aware "self" check is tagged "live" so it surfaces on both
        // the liveness endpoint (/alive, predicate-filtered) and the aggregate
        // /health endpoint (no predicate). Both should fail so orchestrators stop
        // routing new traffic during the drain window.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await gateway.Client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await gateway.Client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Hsts_HeaderEmitted_InProduction_WithDefaultOptions()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production);
        gateway.Client.BaseAddress = new Uri("https://www.example.com/");

        var response = await gateway.Client.GetAsync("/does-not-matter");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_HeaderAbsent_InDevelopment()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development);
        gateway.Client.BaseAddress = new Uri("https://www.example.com/");

        var response = await gateway.Client.GetAsync("/does-not-matter");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Hsts_Disabled_DoesNotEmitHeader_InProduction()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production, new()
        {
            ["Gateway:Hsts:Enabled"] = "false",
        });
        gateway.Client.BaseAddress = new Uri("https://www.example.com/");

        var response = await gateway.Client.GetAsync("/does-not-matter");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task HttpsRedirection_RedirectsTopLevelNavigations_WithDefaultOptions()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production, new()
        {
            ["HTTPS_PORT"] = "443",
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/anywhere");
        request.Headers.Add("Sec-Fetch-Dest", "document");

        var response = await gateway.Client.SendAsync(request);

        Assert.True(IsRedirect(response.StatusCode), $"Expected redirect, got {response.StatusCode}.");
    }

    [Fact]
    public async Task HttpsRedirection_LeavesProgrammaticRequestsAlone()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production, new()
        {
            ["HTTPS_PORT"] = "443",
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/anywhere");
        request.Headers.Add("Sec-Fetch-Dest", "empty");

        var response = await gateway.Client.SendAsync(request);

        Assert.False(IsRedirect(response.StatusCode), $"Did not expect redirect, got {response.StatusCode}.");
    }

    [Fact]
    public async Task HttpsRedirection_Disabled_DoesNotRedirectDocumentNavigations()
    {
        await using var gateway = await StartGatewayAsync(Environments.Production, new()
        {
            ["HTTPS_PORT"] = "443",
            ["Gateway:HttpsRedirection:Enabled"] = "false",
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/anywhere");
        request.Headers.Add("Sec-Fetch-Dest", "document");

        var response = await gateway.Client.SendAsync(request);

        Assert.False(IsRedirect(response.StatusCode), $"Did not expect redirect, got {response.StatusCode}.");
    }

    [Fact]
    public async Task PathBase_MountsEndpointsUnderConfiguredPrefix()
    {
        await using var gateway = await StartGatewayAsync(Environments.Development, new()
        {
            ["Gateway:PathBase"] = "/app",
        });

        Assert.Equal(HttpStatusCode.OK, (await gateway.Client.GetAsync("/app/health")).StatusCode);
    }

    [Fact]
    public async Task ClientApps_ConfigEndpoint_ReturnsConfiguredJson()
    {
        const string json = """{"webAssembly":{"environment":{"OTEL_SERVICE_NAME":"my-app"}}}""";

        await using var gateway = await StartGatewayAsync(Environments.Development, new()
        {
            ["ClientApps:app:ConfigEndpointPath"] = "/myapp/_blazor/_configuration",
            ["ClientApps:app:ConfigResponse"] = json,
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "/myapp/_blazor/_configuration");
        request.Headers.AcceptEncoding.ParseAdd("identity");
        var response = await gateway.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(json, body, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public async Task BuildWebHost_StartsWithHttpsUrl_WhenKestrelCertificateConfigured()
    {
        var builder = WebApplication.CreateSlimBuilder(new[] { "--urls", "https://127.0.0.1:0" });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kestrel:Certificates:Default:Path"] = Path.Combine(AppContext.BaseDirectory, "shared", "TestCertificates", "testCert.pfx"),
            ["Kestrel:Certificates:Default:Password"] = "testPassword",
        });

        await using var app = BlazorGateway.BuildWebHost(builder);

        await app.StartAsync();
    }

    private static bool IsRedirect(HttpStatusCode status) =>
        status is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Found
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static Task<GatewayUnderTest> StartGatewayAsync(string environment) =>
        GatewayTestHelpers.StartGatewayAsync(environment);

    private static Task<GatewayUnderTest> StartGatewayAsync(
        string environment,
        Dictionary<string, string?> configuration) =>
        GatewayTestHelpers.StartGatewayAsync(environment, configuration);
}
