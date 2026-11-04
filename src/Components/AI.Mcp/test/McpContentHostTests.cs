// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;

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
    public async Task HostContextByteLimitIsInclusiveBeforeAttachment(int allowance)
    {
        const string json = """{"locale":"es-ES"}""";
        await using var host = new TestHost(Encoding.UTF8.GetByteCount(json) + allowance);
        using var document = JsonDocument.Parse(json);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.UpdateAsync(document.RootElement).AsTask());

        Assert.Equal(allowance < 0
            ? "The MCP host-context update exceeds the configured byte limit."
            : "The MCP app is not ready.", error.Message);
    }

    private sealed class TestHost(int maximumMessageBytes = 1024) : McpContentHost
    {
        protected override McpContentOptions Options { get; } = new()
        {
            ProxyUrl = new("https://proxy.example/proxy"),
            MaximumMessageBytes = maximumMessageBytes,
        };

        public ValueTask UpdateAsync(JsonElement context) => UpdateHostContextAsync(context);
    }
}
