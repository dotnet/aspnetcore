// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentHostDispatchTests
{
    [Fact]
    public async Task AliasedToolSnapshotUsesOriginalNamesAndSuppliedClient()
    {
        var firstServer = new McpClientFixture("same");
        var secondServer = new McpClientFixture("same");
        await using var first = await McpClient.CreateAsync(firstServer);
        await using var second = await McpClient.CreateAsync(secondServer);
        var tools = (await second.ListToolsAsync()).Select(tool => tool.WithName("alias_" + tool.Name)).ToArray();
        await using var view = new RequestHost(second, tools, "alias_open");

        var response = await view.DispatchAsync(Request("tools/call", """{"name":"action","arguments":{"value":"changed"}}"""), default);
        var listed = await view.DispatchAsync(Request("tools/list", "{}"), default);

        Assert.IsType<JsonRpcResponse>(response);
        Assert.Equal(0, firstServer.ToolCalls);
        Assert.Equal(1, secondServer.ToolCalls);
        Assert.Equal("action", secondServer.LastToolName);
        Assert.Equal("changed", secondServer.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal(new[] { "open", "action" }, Assert.IsType<JsonRpcResponse>(listed).Result!["tools"]!.AsArray()
            .Select(tool => tool!["name"]!.GetValue<string>()).ToArray());
        Assert.Equal(1, secondServer.ToolLists);
    }

    [Fact]
    public async Task ResourcesRemainRestrictedToSelectedAppOnSuppliedClient()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        await using var view = new RequestHost(client, (await client.ListToolsAsync()).ToArray(), "open");

        var denied = await view.DispatchAsync(Request("resources/read", """{"uri":"data://test/item"}"""), default);
        var read = await view.DispatchAsync(Request("resources/read", """{"uri":"ui://test/app"}"""), default);
        var listed = await view.DispatchAsync(Request("resources/list", "{}"), default);

        Assert.Equal(-32000, Assert.IsType<JsonRpcError>(denied).Error.Code);
        Assert.Equal("ui://test/app", Assert.IsType<JsonRpcResponse>(read).Result!["contents"]![0]!["uri"]!.GetValue<string>());
        Assert.Single(Assert.IsType<JsonRpcResponse>(listed).Result!["resources"]!.AsArray());
        Assert.Equal(1, server.ResourceReads);
        Assert.Equal(1, server.ResourceLists);
    }

    [Fact]
    public async Task ModelAliasesCannotSelectServerToolsOrOtherResources()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var tools = (await client.ListToolsAsync()).Select(tool => tool.WithName("alias_" + tool.Name)).ToArray();
        await using var view = new RequestHost(client, tools, "alias_open");

        var unlisted = await view.DispatchAsync(Request("tools/call", """{"name":"missing","arguments":{}}"""), default);
        var alias = await view.DispatchAsync(Request("tools/call", """{"name":"alias_open","arguments":{}}"""), default);
        var resource = await view.DispatchAsync(Request("resources/read", """{"uri":"file:///not-granted"}"""), default);
        var unsupported = await view.DispatchAsync(Request("$initialResult", "{}"), default);

        Assert.Equal(-32000, Assert.IsType<JsonRpcError>(unlisted).Error.Code);
        Assert.Equal(-32000, Assert.IsType<JsonRpcError>(alias).Error.Code);
        Assert.Equal(-32000, Assert.IsType<JsonRpcError>(resource).Error.Code);
        Assert.Equal(-32601, Assert.IsType<JsonRpcError>(unsupported).Error.Code);
        Assert.Equal(0, server.ToolCalls);
    }

    [Theory]
    [InlineData("tools/call", """{"name":"action","arguments":null}""")]
    [InlineData("tools/call", """{"name":"action","arguments":[]}""")]
    [InlineData("tools/call", """{"name":"action","arguments":"invalid"}""")]
    [InlineData("tools/call", """{"name":"action","arguments":1}""")]
    [InlineData("tools/call", """{"name":"action","arguments":true}""")]
    [InlineData("tools/call", """{}""")]
    [InlineData("tools/call", """{"name":null}""")]
    [InlineData("tools/call", """{"name":1}""")]
    [InlineData("tools/call", """{"name":[]}""")]
    [InlineData("tools/call", """{"name":""}""")]
    [InlineData("tools/call", """[]""")]
    [InlineData("resources/read", """{}""")]
    [InlineData("resources/read", """{"uri":null}""")]
    [InlineData("resources/read", """{"uri":1}""")]
    [InlineData("resources/read", """{"uri":[]}""")]
    [InlineData("resources/read", """{"uri":""}""")]
    [InlineData("resources/read", """null""")]
    [InlineData("tools/list", """[]""")]
    [InlineData("resources/list", """false""")]
    [InlineData("", """{}""")]
    public async Task MalformedParametersReturnInvalidParams(string method, string json)
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        await using var view = new RequestHost(client, (await client.ListToolsAsync()).ToArray(), "open");

        var response = await view.DispatchAsync(Request(method, json), default);

        var error = Assert.IsType<JsonRpcError>(response);
        Assert.Equal(new RequestId("request"), error.Id);
        Assert.Equal(-32602, error.Error.Code);
        Assert.Equal(0, server.ToolCalls);
    }

    [Fact]
    public async Task DisposingViewDoesNotDisposeClient()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var view = new RequestHost(client, (await client.ListToolsAsync()).ToArray(), "open");

        await view.DisposeAsync();
        await client.PingAsync();

        Assert.False(server.Disposed);
    }

    private static JsonRpcRequest Request(string method, string json) =>
        new() { Id = new("request"), Method = method, Params = JsonNode.Parse(json) };

    private sealed class RequestHost : McpContentHost
    {
        public RequestHost(McpClient client, IReadOnlyList<McpClientTool> tools, string name)
        {
            Client = client;
            Tools = tools;
            Block = new() { Call = new("call", name, new Dictionary<string, object?>()) };
            OnParametersSet();
        }

    }
}
