// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// End-to-end validation that the <c>Microsoft.AspNetCore.Components.Gateway.Cli</c> package can be
/// installed as a dotnet tool and that the resulting <c>blazor-gateway</c> command runs and serves
/// requests. These tests install the locally-built package into a throwaway tool-path and launch the
/// real gateway process, so they only run where the package has been packed (and not on Helix).
/// </summary>
[RequiresBuiltGatewayCliPackage]
public class BlazorGatewayCliToolTests
{
    [ConditionalFact]
    public void Tool_Installs_AndExposesBlazorGatewayCommand()
    {
        using var tool = GatewayToolInstallation.Install();

        Assert.True(
            File.Exists(tool.CommandPath),
            $"Expected the installed tool to expose the '{GatewayCliTestData.ToolCommandName}' command at '{tool.CommandPath}'.");
    }

    [ConditionalFact]
    public async Task Tool_Runs_AndAnswersLivenessProbe()
    {
        using var tool = GatewayToolInstallation.Install();

        await using var running = await tool.StartAsync("--environment", "Development");

        // The liveness endpoint is mapped in every environment, so a 200 here proves the installed
        // tool launched the real gateway and is serving requests.
        var alive = await running.Client.GetAsync("/alive");
        Assert.Equal(HttpStatusCode.OK, alive.StatusCode);

        // The aggregate health endpoint is only mapped in Development.
        var health = await running.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [ConditionalFact]
    public async Task Tool_HostsConfiguredClientApp_AndServesItsConfiguration()
    {
        const string configJson = """{"webAssembly":{"environment":{"OTEL_SERVICE_NAME":"my-app"}}}""";

        using var tool = GatewayToolInstallation.Install();

        // Drive the gateway exactly the way Aspire's Blazor integration does: pass a client app's
        // configuration on the command line and confirm the running tool serves it back.
        await using var running = await tool.StartAsync(
            "--environment", "Development",
            "--ClientApps:app:ConfigEndpointPath", "/myapp/_blazor/_configuration",
            "--ClientApps:app:ConfigResponse", $"\"{configJson.Replace("\"", "\\\"")}\"");

        var request = new HttpRequestMessage(HttpMethod.Get, "/myapp/_blazor/_configuration");
        request.Headers.AcceptEncoding.ParseAdd("identity");

        var response = await running.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(configJson, body, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public async Task Tool_ReportsNativeAotWithDynamicCodeDisabled_OnSupportedHost()
    {
        if (!GatewayCliTestData.IsNativePackageAvailable)
        {
            return;
        }

        using var tool = GatewayToolInstallation.Install();

        var result = await tool.RunAsync("--info");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"RID: {GatewayCliTestData.HostRuntimeIdentifier}", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("Dynamic code supported: False", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain(".NET Framework", result.StandardOutput, StringComparison.Ordinal);
    }

    [ConditionalFact]
    public async Task Tool_PreservesGatewayFeatures_OnNativeAotHost()
    {
        if (!GatewayCliTestData.IsNativePackageAvailable)
        {
            return;
        }

        var traceparent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exportedTelemetry = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var upstream = await GatewayTestHelpers.StartUpstreamAsync(app =>
        {
            app.MapGet("/proxy/{**path}", (HttpContext context) =>
            {
                traceparent.TrySetResult(context.Request.Headers["traceparent"].ToString());
                return "proxied";
            });
            app.MapPost("/v1/{signal}", (string signal) =>
            {
                if (string.Equals(signal, "traces", StringComparison.Ordinal))
                {
                    exportedTelemetry.TrySetResult(signal);
                }

                return Results.Ok();
            });
        });
        var upstreamUri = new Uri(upstream.BaseUrl);

        using var tool = GatewayToolInstallation.Install();
        var manifestPath = Path.Combine(
            tool.SelectedToolDirectory,
            "blazor-gateway.staticwebassets.endpoints.json");

        await using var running = await tool.StartAsync(
            "--environment", "Development",
            "--ClientApps:app:ConfigEndpointPath", "/app/_blazor/_configuration",
            "--ClientApps:app:ConfigResponse", "\"{\\\"enabled\\\":true}\"",
            "--ClientApps:app:EndpointsManifest", $"\"{manifestPath}\"",
            "--ReverseProxy:Routes:proxy:ClusterId", "upstream",
            "--ReverseProxy:Routes:proxy:Match:Path", "/proxy/{**catch-all}",
            "--ReverseProxy:Clusters:upstream:Destinations:primary:Address", "http://upstream",
            "--services:upstream:default:0", $"{upstreamUri.Host}:{upstreamUri.Port}",
            "--OTEL_EXPORTER_OTLP_ENDPOINT", upstream.BaseUrl,
            "--OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf",
            "--OTEL_BSP_SCHEDULE_DELAY", "100");

        Assert.Equal(HttpStatusCode.OK, (await running.Client.GetAsync("/alive")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await running.Client.GetAsync("/health")).StatusCode);

        var configRequest = new HttpRequestMessage(HttpMethod.Get, "/app/_blazor/_configuration");
        configRequest.Headers.AcceptEncoding.ParseAdd("identity");
        var configResponse = await running.Client.SendAsync(configRequest);
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);
        Assert.Equal("""{"enabled":true}""", await configResponse.Content.ReadAsStringAsync());

        var staticAsset = await running.Client.GetAsync("/_framework/blazor.web.js");
        Assert.Equal(HttpStatusCode.OK, staticAsset.StatusCode);

        var proxy = await running.Client.GetAsync("/proxy/echo");
        Assert.Equal(HttpStatusCode.OK, proxy.StatusCode);
        Assert.Equal("proxied", await proxy.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(await traceparent.Task.WaitAsync(TimeSpan.FromSeconds(10))));
        Assert.Equal("traces", await exportedTelemetry.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [ConditionalFact]
    public async Task Tool_ServesHttps_OnNativeAotHost()
    {
        if (!GatewayCliTestData.IsNativePackageAvailable)
        {
            return;
        }

        using var tool = GatewayToolInstallation.Install();
        var certificatePath = Path.Combine(
            GatewayCliTestData.RepoRoot,
            "src",
            "Shared",
            "TestCertificates",
            "testCert.pfx");

        await using var running = await tool.StartAsync(
            useHttps: true,
            "--environment", "Development",
            "--Kestrel:Certificates:Default:Path", $"\"{certificatePath}\"",
            "--Kestrel:Certificates:Default:Password", "testPassword");

        Assert.StartsWith("https://", running.ListeningUrl, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await running.Client.GetAsync("/alive")).StatusCode);
    }
}
