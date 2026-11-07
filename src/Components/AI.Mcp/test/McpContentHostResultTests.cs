// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentHostResultTests
{
    [Fact]
    public async Task OrdinaryPipelinePreservesSdkEnvelopeAndMetadata()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var tools = (await client.ListToolsAsync()).ToArray();
        using var model = new ToolCallingClient();
        using var chat = new FunctionInvokingChatClient(model);
        using var agent = new UIAgent(chat, new ChatOptions { Tools = [tools[0]] });
        var blocks = new List<FunctionInvocationContentBlock>();

        await foreach (var block in agent.SendMessageAsync(new(ChatRole.User, "Open")))
        {
            if (block is FunctionInvocationContentBlock invocation)
            {
                blocks.Add(invocation);
            }
        }

        var completed = Assert.Single(blocks);
        Assert.Equal("original-call", completed.Id);
        var normalResult = Assert.IsType<JsonElement>(completed.Result!.Result);
        var delivered = await DeliverAsync(client, tools, completed.Result.Result);
        Assert.Equal("Not for the model.", normalResult.GetProperty("_meta").GetProperty("viewOnly").GetString());
        Assert.Equal(normalResult.GetRawText(), delivered.GetRawText());
        Assert.Equal(JsonSerializer.SerializeToElement(server.Result, McpJsonUtilities.DefaultOptions).GetRawText(), delivered.GetRawText());
        Assert.Same(completed.Result, model.ToolResult);
        Assert.IsNotType<CallToolResult>(completed.Result.RawRepresentation);
        Assert.Equal(1, server.ToolCalls);
        Assert.Equal(2, model.Requests);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SdkConvertedContentPreservesIndividualProtocolBlocks(int count)
    {
        var first = new TextContentBlock
        {
            Text = "Text",
            Meta = new() { ["contentMetadata"] = "Preserved" }
        };
        var second = ImageContentBlock.FromBytes(new byte[] { 1, 2 }, "image/png");
        var server = new McpClientFixture("server")
        {
            Result = new() { Content = count == 1 ? [first] : [first, second] }
        };
        await using var client = await McpClient.CreateAsync(server);
        var tool = (await client.ListToolsAsync())[0];

        var normalResult = await tool.InvokeAsync(new());
        var delivered = await DeliverAsync(client, [tool], normalResult);

        Assert.Equal(count, delivered.GetProperty("content").GetArrayLength());
        Assert.Equal("Preserved", delivered.GetProperty("content")[0].GetProperty("_meta").GetProperty("contentMetadata").GetString());
        Assert.Equal(JsonSerializer.SerializeToElement(server.Result, McpJsonUtilities.DefaultOptions).GetRawText(), delivered.GetRawText());
        Assert.Equal("Preserved", first.Meta!["contentMetadata"]!.GetValue<string>());
        Assert.Equal(1, server.ToolCalls);
    }

    [Fact]
    public async Task SdkMcpErrorIsDeliveredAsAnErrorResult()
    {
        var server = new McpClientFixture("server")
        {
            Result = new()
            {
                Content = [new TextContentBlock { Text = "Business failure" }],
                IsError = true,
                Meta = new() { ["viewOnly"] = "Error detail" }
            }
        };
        await using var client = await McpClient.CreateAsync(server);
        var tool = (await client.ListToolsAsync())[0];

        var result = await DeliverAsync(client, [tool], await tool.InvokeAsync(new()));

        Assert.True(result.GetProperty("isError").GetBoolean());
        Assert.Equal("Error detail", result.GetProperty("_meta").GetProperty("viewOnly").GetString());
        Assert.Equal("Business failure", result.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task CompleteEnvelopePreservesNestedMetadataAndExtensionFields()
    {
        const string json = """
            {"content":[{"type":"resource","resource":{"uri":"note://details","text":"Details","_meta":{"viewOnly":true}}}],
             "structuredContent":{"_meta":{"business":42}},"_meta":{"app":"retained"},"extension":"retained"}
            """;
        using var document = JsonDocument.Parse(json);
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);

        var delivered = await DeliverAsync(client, (await client.ListToolsAsync()).ToArray(), document.RootElement);

        Assert.Equal(document.RootElement.GetRawText(), delivered.GetRawText());
        Assert.Equal("retained", delivered.GetProperty("extension").GetString());
        Assert.Equal("retained", delivered.GetProperty("_meta").GetProperty("app").GetString());
    }

    [Fact]
    public async Task ProtocolResultIsNotMutated()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Text", Meta = new() { ["detail"] = true } }],
            StructuredContent = JsonSerializer.SerializeToElement(new { value = 42 }),
            Meta = new() { ["app"] = "Metadata" }
        };
        var before = JsonSerializer.SerializeToElement(result, McpJsonUtilities.DefaultOptions).GetRawText();
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);

        var delivered = await DeliverAsync(client, (await client.ListToolsAsync()).ToArray(), result);

        Assert.Equal(before, delivered.GetRawText());
        Assert.Equal(before, JsonSerializer.SerializeToElement(result, McpJsonUtilities.DefaultOptions).GetRawText());
    }

    [Fact]
    public async Task UnsupportedShapesProduceCancellationInsteadOfSuccess()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var tools = (await client.ListToolsAsync()).ToArray();
        object?[] results =
        [
            null,
            "Not an SDK content result",
            JsonSerializer.SerializeToElement(new { business = "Not an MCP result envelope" }),
            new FunctionCallContent("call", "function", new Dictionary<string, object?>())
        ];
        foreach (var result in results)
        {
            await using var host = new McpHostFixture(client, tools, Block(tools[0], result));

            await host.InitializeAsync();

            Assert.Empty(host.Browser.Results);
            var reason = Assert.Single(host.Browser.Cancellations);
            Assert.Contains(result?.GetType().FullName ?? "null", reason);
        }
    }

    [Fact]
    public async Task LargeOriginalResultIsDeliveredIntact()
    {
        var server = new McpClientFixture("server");
        await using var client = await McpClient.CreateAsync(server);
        var text = new string('x', 2 * 1024 * 1024 + 1);
        var result = new CallToolResult { Content = [new TextContentBlock { Text = text }] };

        var delivered = await DeliverAsync(client, (await client.ListToolsAsync()).ToArray(), result);

        Assert.Equal(text, delivered.GetProperty("content")[0].GetProperty("text").GetString());
    }

    private static FunctionInvocationContentBlock Block(McpClientTool tool, object? result) => new()
    {
        Call = new("call", tool.Name, new Dictionary<string, object?>()),
        Result = new("call", result)
    };

    private static async Task<JsonElement> DeliverAsync(McpClient client, IReadOnlyList<McpClientTool> tools, object? result)
    {
        await using var host = new McpHostFixture(client, tools, Block(tools[0], result));
        await host.InitializeAsync();
        Assert.Empty(host.Browser.Cancellations);
        return Assert.Single(host.Browser.Results);
    }

    private sealed class ToolCallingClient : IChatClient
    {
        public int Requests { get; private set; }
        public FunctionResultContent? ToolResult { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Requests++;
            if (Requests == 1)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("original-call", "open", new Dictionary<string, object?> { ["value"] = "original" })])));
            }
            ToolResult = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Single();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var update in (await GetResponseAsync(messages, options, cancellationToken)).ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(IChatClient) ? this : null;

        public void Dispose()
        {
        }
    }
}
