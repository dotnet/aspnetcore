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
}
