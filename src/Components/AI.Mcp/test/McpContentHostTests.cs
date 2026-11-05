// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Linq;
using ModelContextProtocol.Client;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentHostTests
{
    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("\"dark\"")]
    public async Task HostContextUpdateRequiresJsonObject(string json)
    {
        await using var host = new TestHost();
        using var document = JsonDocument.Parse(json);

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            host.UpdateAsync(document.RootElement).AsTask());

        Assert.Equal("hostContext", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task LargeHostContextReachesReadinessCheckWithoutSizeRejection(int allowance)
    {
        var json = "{\"locale\":\"" + new string('x', 2 * 1024 * 1024 - 13 + allowance) + "\"}";
        Assert.Equal(2 * 1024 * 1024 + allowance, Encoding.UTF8.GetByteCount(json));
        await using var host = new TestHost();
        using var document = JsonDocument.Parse(json);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.UpdateAsync(document.RootElement).AsTask());

        Assert.Equal("The MCP app is not ready.", error.Message);
    }

    [Fact]
    public async Task LargeHostContextIsDeliveredWithoutSizeLimit()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());
        var locale = new string('x', 2 * 1024 * 1024 + 1);
        var context = JsonSerializer.SerializeToElement(new { locale });
        await host.InitializeAsync();

        await host.Host.UpdateAsync(context);

        Assert.Equal(locale, Assert.Single(host.Browser.Contexts).GetProperty("locale").GetString());
    }

    [Fact]
    public async Task CleanupDoesNotExpireAfterFiveSeconds()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await host.InitializeAsync();
        host.Browser.DisposeCompletion = release.Task;
        var closing = host.Host.DisposeAsync().AsTask();

        await Task.Delay(TimeSpan.FromSeconds(6));
        release.SetResult();

        await closing;
        await client.PingAsync();
        Assert.False(server.Disposed);
    }

    private sealed class TestHost : McpContentHost
    {
        public ValueTask UpdateAsync(JsonElement context) => UpdateHostContextAsync(context);
    }
}
