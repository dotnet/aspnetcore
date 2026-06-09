// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Gateway;

public class BlazorGatewayProxyTests
{
    [Fact]
    public async Task ReverseProxy_NotMapped_WhenSectionMissing()
    {
        await using var gateway = await GatewayTestHelpers.StartGatewayAsync(Environments.Development);

        // No ReverseProxy config and no ClientApps → unknown path is 404.
        Assert.Equal(HttpStatusCode.NotFound, (await gateway.Client.GetAsync("/anything")).StatusCode);
    }

    [Fact]
    public async Task ReverseProxy_ForwardsRequestToConfiguredUpstream()
    {
        await using var upstream = await GatewayTestHelpers.StartUpstreamAsync(app =>
            app.MapGet("/api/echo", () => "hello from upstream"));

        await using var gateway = await GatewayTestHelpers.StartGatewayAsync(
            Environments.Development,
            ProxyConfig("upstream", upstream.BaseUrl, route: "/api/{**catch-all}"));

        var response = await gateway.Client.GetAsync("/api/echo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("hello from upstream", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReverseProxy_PreservesRequestPathAndQuery()
    {
        await using var upstream = await GatewayTestHelpers.StartUpstreamAsync(app =>
            app.MapGet("/api/items/{id}", (string id, HttpRequest request) =>
                $"{id}{request.QueryString.Value}"));

        await using var gateway = await GatewayTestHelpers.StartGatewayAsync(
            Environments.Development,
            ProxyConfig("upstream", upstream.BaseUrl, route: "/api/{**catch-all}"));

        var response = await gateway.Client.GetAsync("/api/items/42?expand=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("42?expand=true", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ServiceDiscovery_ResolvesAddressFromConfiguration()
    {
        await using var upstream = await GatewayTestHelpers.StartUpstreamAsync(app =>
            app.MapGet("/api/ping", () => "pong"));

        var upstreamUri = new Uri(upstream.BaseUrl);

        // Microsoft.Extensions.ServiceDiscovery reads endpoints from configuration keys of the
        // form `services:<service>:<endpoint>:<index>`. YARP's service-discovery destination
        // resolver translates `http://upstream` into the configured `host:port` for endpoint
        // `default` (when present) or matched by scheme.
        var cfg = ProxyConfig("upstream", "http://upstream", route: "/api/{**catch-all}");
        cfg["services:upstream:default:0"] = $"{upstreamUri.Host}:{upstreamUri.Port}";

        await using var gateway = await GatewayTestHelpers.StartGatewayAsync(Environments.Development, cfg);

        var response = await gateway.Client.GetAsync("/api/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ServiceDiscovery_Disabled_LeavesUnresolvedAddress()
    {
        var cfg = ProxyConfig("upstream", "http://_does-not-exist", route: "/api/{**catch-all}");
        cfg["Gateway:HttpClient:ServiceDiscovery"] = "false";

        await using var gateway = await GatewayTestHelpers.StartGatewayAsync(Environments.Development, cfg);

        // Without service discovery the scheme-less host segment isn't translated, so the
        // proxy fails to dispatch (502/503/504 depending on YARP error mapping).
        var response = await gateway.Client.GetAsync("/api/whatever");

        Assert.True(
            response.StatusCode is HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.GatewayTimeout,
            $"Expected gateway error, got {response.StatusCode}.");
    }

    private static Dictionary<string, string?> ProxyConfig(
        string clusterId,
        string destinationAddress,
        string route)
    {
        var routeId = $"{clusterId}-route";

        return new Dictionary<string, string?>
        {
            [$"ReverseProxy:Routes:{routeId}:ClusterId"] = clusterId,
            [$"ReverseProxy:Routes:{routeId}:Match:Path"] = route,
            [$"ReverseProxy:Clusters:{clusterId}:Destinations:dest1:Address"] = destinationAddress,
        };
    }
}
