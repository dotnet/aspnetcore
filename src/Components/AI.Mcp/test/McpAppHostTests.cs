// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpAppHostTests
{
    [Fact]
    public async Task DefaultSdkClientUsesDiscoveryHandshake()
    {
        var server = new ProtocolFixture("first");

        await using var client = await McpClient.CreateAsync(server);

        Assert.Equal(1, server.DiscoveryRequests);
        Assert.Equal(0, server.InitializeRequests);
        Assert.Equal("2026-07-28", client.NegotiatedProtocolVersion);
        Assert.Equal("first", client.ServerInfo.Name);
        Assert.Equal("1", client.ServerInfo.Version);
        Assert.NotNull(client.ServerCapabilities.Tools);
        Assert.NotNull(client.ServerCapabilities.Resources);
    }

    [Fact]
    public async Task ExistingAgentPipelineMapsTheOriginalCallAndProjectedResult()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        var functions = host.BindTools(client, CreateTools(client));
        var model = new ToolCallingClient("model_alias");
        using var chat = new FunctionInvokingChatClient(model);
        using var agent = new UIAgent(chat, options =>
        {
            host.Configure(options);
            options.ChatOptions = new() { Tools = [.. functions] };
        });
        var blocks = new List<Microsoft.AspNetCore.Components.AI.ContentBlock>();

        await foreach (var block in agent.SendMessageAsync(new(ChatRole.User, "Open the app")))
        {
            blocks.Add(block);
        }

        var app = Assert.Single(blocks.OfType<McpAppContentBlock>());
        Assert.Equal(model.Call.CallId, app.Id);
        Assert.Equal("model_alias", app.ToolName);
        Assert.True(app.HasResult);
        Assert.Equal(model.Call.CallId, app.Result!.CallId);
        Assert.Equal(BlockLifecycleState.Inactive, app.LifecycleState);
        var invocation = Assert.IsType<McpAppInvocation>(host.FindInvocation(app));
        Assert.Same(client, invocation.Binding.Connection.Client);
        Assert.NotNull((await invocation.Result).Meta!["ui"]);
        Assert.Equal(1, server.ToolCalls);
        Assert.Equal(2, model.Requests);
    }

    [Fact]
    public async Task NativeModelInvocationRetainsRawBeforeProjectionAndNeverReplaysOnRead()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        var functions = host.BindTools(client, CreateTools(client));
        var model = new ToolCallingClient("model_alias");
        using var chat = new FunctionInvokingChatClient(model);

        await chat.GetResponseAsync([new(ChatRole.User, "Open the app")], new() { Tools = [.. functions] });

        var invocation = Assert.IsType<McpAppInvocation>(host.FindInvocation(new()
        {
            Call = model.Call,
        }));
        var raw = await invocation.Result;
        Assert.Equal("private-ui-data", raw.Meta!["ui"]!["private"]!.GetValue<string>());
        var projected = Assert.IsType<JsonElement>(model.ToolResult!.Result);
        Assert.False(projected.TryGetProperty("_meta", out _));
        Assert.Equal("business", raw.Meta["trace"]!.GetValue<string>());
        Assert.Equal("first", projected.GetProperty("structuredContent").GetProperty("server").GetString());
        Assert.Equal("model_alias", invocation.Call.Name);
        Assert.Equal("open", server.LastToolName);

        var initial = await host.DispatchAsync(invocation, Request("$initialResult", "{}"), ViewOptions(), default);
        var again = await host.DispatchAsync(invocation, Request("$initialResult", "{}"), ViewOptions(), default);

        Assert.Equal("private-ui-data", initial.Result!.Value.GetProperty("_meta").GetProperty("ui").GetProperty("private").GetString());
        Assert.Equal(initial.Result.Value.GetRawText(), again.Result!.Value.GetRawText());
        Assert.Equal(1, server.ToolCalls);
        Assert.Equal(2, model.Requests);
    }

    [Fact]
    public async Task AppActionUsesOwningConnectionAndOriginalNamesWithoutCallingModel()
    {
        var firstServer = new ProtocolFixture("first");
        var secondServer = new ProtocolFixture("second");
        await using var first = await McpClient.CreateAsync(firstServer);
        await using var second = await McpClient.CreateAsync(secondServer);
        using var host = CreateHost();
        var firstFunctions = host.BindTools(first, CreateTools(first));
        host.BindTools(second, CreateTools(second, "other_alias"));
        var model = new ToolCallingClient("model_alias");
        using var chat = new FunctionInvokingChatClient(model);
        await chat.GetResponseAsync([new(ChatRole.User, "Open")], new() { Tools = [.. firstFunctions] });
        var invocation = host.FindInvocation(new() { Call = model.Call })!;

        var response = await host.DispatchAsync(invocation, Request("tools/call", """{"name":"action","arguments":{"value":"changed"}}"""), ViewOptions(), default);

        Assert.Equal("first", response.Result!.Value.GetProperty("structuredContent").GetProperty("server").GetString());
        Assert.Equal(2, firstServer.ToolCalls);
        Assert.Equal(0, secondServer.ToolCalls);
        Assert.Equal("action", firstServer.LastToolName);
        Assert.Equal("changed", firstServer.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal(2, model.Requests);
        Assert.Single(firstFunctions);
        var listed = await host.DispatchAsync(invocation, Request("tools/list", "{}"), ViewOptions(), default);
        Assert.Equal(new[] { "open", "action" }, listed.Result!.Value.GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()).ToArray());
    }

    [Fact]
    public async Task SameClientCanBeBoundWithDifferentModelAliases()
    {
        var server = new ProtocolFixture("same");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        var functions = host.BindTools(client, CreateTools(client)).ToList();
        functions.AddRange(host.BindTools(client, CreateTools(client, "other_alias")));
        var model = new ToolCallingClient("other_alias");
        using var chat = new FunctionInvokingChatClient(model);

        await chat.GetResponseAsync([new(ChatRole.User, "Open")], new() { Tools = [.. functions] });
        var invocation = Assert.IsType<McpAppInvocation>(host.FindInvocation(new() { Call = model.Call }));
        var response = await host.DispatchAsync(invocation,
            Request("tools/call", """{"name":"action","arguments":{"value":"changed"}}"""), ViewOptions(), default);

        Assert.Null(response.Error);
        Assert.Equal(new[] { "model_alias", "other_alias" }, functions.Select(function => function.Name).ToArray());
        Assert.Same(client, host.FindAppTool("model_alias")!.Connection.Client);
        Assert.Same(client, invocation.Binding.Connection.Client);
        Assert.Equal(2, server.ToolCalls);
        Assert.Equal("action", server.LastToolName);
        Assert.Equal("changed", server.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal(2, model.Requests);
        Assert.Throws<ArgumentException>(() => host.BindTools(client, CreateTools(client)));
    }

    [Fact]
    public async Task ModelNameCollisionDoesNotPreventBindingTheClientWithAnAlias()
    {
        var firstServer = new ProtocolFixture("same");
        var secondServer = new ProtocolFixture("same");
        await using var first = await McpClient.CreateAsync(firstServer);
        await using var second = await McpClient.CreateAsync(secondServer);
        using var host = CreateHost();
        host.BindTools(first, CreateTools(first));

        Assert.Throws<ArgumentException>(() => host.BindTools(second, CreateTools(second)));
        var functions = host.BindTools(second, CreateTools(second, "other_alias"));

        Assert.Equal("other_alias", Assert.Single(functions).Name);
        Assert.Same(first, host.FindAppTool("model_alias")!.Connection.Client);
        Assert.Same(second, host.FindAppTool("other_alias")!.Connection.Client);
    }

    [Fact]
    public async Task ClientsWithIdenticalServerIdentityRouteIndependentlyAndReachToolPolicy()
    {
        var firstServer = new ProtocolFixture("same");
        var secondServer = new ProtocolFixture("same");
        await using var first = await McpClient.CreateAsync(firstServer);
        await using var second = await McpClient.CreateAsync(secondServer);
        using var host = CreateHost();
        host.BindTools(first, CreateTools(first));
        host.BindTools(second, CreateTools(second, "other_alias"));
        Assert.Equal(first.ServerInfo.Name, second.ServerInfo.Name);
        Assert.Equal(first.ServerInfo.Version, second.ServerInfo.Version);
        var expectedClient = first;
        var policyCalls = 0;
        var options = ViewOptions();
        options.AllowToolCall = (client, name) =>
        {
            Assert.Same(expectedClient, client);
            Assert.Equal("action", name);
            policyCalls++;
            return true;
        };
        var firstInvocation = host.StartInvocation(host.FindAppTool("model_alias")!,
            new("first-call", "model_alias", new Dictionary<string, object?>()));
        var secondInvocation = host.StartInvocation(host.FindAppTool("other_alias")!,
            new("second-call", "other_alias", new Dictionary<string, object?>()));
        var request = Request("tools/call", """{"name":"action","arguments":{"value":"first"}}""");

        var firstResponse = await host.DispatchAsync(firstInvocation, request, options, default);
        Assert.Null(firstResponse.Error);
        Assert.Equal(1, firstServer.ToolCalls);
        Assert.Equal(0, secondServer.ToolCalls);

        expectedClient = second;
        var secondResponse = await host.DispatchAsync(secondInvocation,
            Request("tools/call", """{"name":"action","arguments":{"value":"second"}}"""), options, default);

        Assert.Null(secondResponse.Error);
        Assert.Equal(1, firstServer.ToolCalls);
        Assert.Equal(1, secondServer.ToolCalls);
        Assert.Equal("first", firstServer.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal("second", secondServer.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal(2, policyCalls);
    }

    [Fact]
    public async Task ResourcePoliciesReceiveOwningClientForReadsAndListing()
    {
        var firstServer = new ProtocolFixture("same");
        var secondServer = new ProtocolFixture("same");
        await using var first = await McpClient.CreateAsync(firstServer);
        await using var second = await McpClient.CreateAsync(secondServer);
        using var host = CreateHost();
        host.BindTools(first, CreateTools(first));
        host.BindTools(second, CreateTools(second, "other_alias"));
        var invocation = host.StartInvocation(host.FindAppTool("other_alias")!,
            new("call", "other_alias", new Dictionary<string, object?>()));
        var options = ViewOptions();
        var policyCalls = 0;
        options.AllowResourceRead = (client, uri) =>
        {
            Assert.Same(second, client);
            Assert.Equal("data://test/item", uri);
            policyCalls++;
            return true;
        };

        var read = await host.DispatchAsync(invocation,
            Request("resources/read", """{"uri":"data://test/item"}"""), options, default);
        var listed = await host.DispatchAsync(invocation, Request("resources/list", "{}"), options, default);

        Assert.Null(read.Error);
        Assert.Equal("data://test/item", read.Result!.Value.GetProperty("contents")[0].GetProperty("uri").GetString());
        Assert.Null(listed.Error);
        Assert.Equal(new[] { "ui://test/app", "data://test/item" }, listed.Result!.Value.GetProperty("resources")
            .EnumerateArray().Select(resource => resource.GetProperty("uri").GetString()).ToArray());
        Assert.Equal(0, firstServer.ResourceReads);
        Assert.Equal(0, firstServer.ResourceLists);
        Assert.Equal(1, secondServer.ResourceReads);
        Assert.Equal(1, secondServer.ResourceLists);
        Assert.Equal(2, policyCalls);
    }

    [Fact]
    public async Task MissingNativeCallIdentityFailsBeforeSendingToolRequest()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        var function = Assert.Single(host.BindTools(client, CreateTools(client)));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => await function.InvokeAsync(new()));

        Assert.Contains("native function-call identity", error.Message);
        Assert.Equal(0, server.ToolCalls);
    }

    [Fact]
    public async Task HostPolicyRejectsUnpermittedToolsAndResourcesBeforeDispatch()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        var options = ViewOptions();
        using var host = CreateHost();
        host.BindTools(client, CreateTools(client));
        var binding = host.FindAppTool("model_alias")!;
        var invocation = host.StartInvocation(binding, new("call", "model_alias", new Dictionary<string, object?>()));

        options.AllowToolCall = static (_, _) => false;
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await host.DispatchAsync(invocation, Request("tools/call", """{"name":"action","arguments":{"value":"no"}}"""), options, default));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await host.DispatchAsync(invocation, Request("tools/call", """{"name":"model_alias","arguments":{}}"""), options, default));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await host.DispatchAsync(invocation, Request("resources/read", """{"uri":"file:///not-granted"}"""), options, default));

        Assert.Equal(0, server.ToolCalls);
        var unsupported = await host.DispatchAsync(invocation, Request("ui/message", "{}"), options, default);
        Assert.Equal(-32601, unsupported.Error!.Code);
    }

    [Fact]
    public async Task ContentHostsCanApplyDifferentPoliciesToTheSameInvocation()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        host.BindTools(client, CreateTools(client));
        var invocation = host.StartInvocation(host.FindAppTool("model_alias")!,
            new("call", "model_alias", new Dictionary<string, object?>()));
        var denied = ViewOptions();
        denied.AllowToolCall = static (_, _) => false;
        var request = Request("tools/call", """{"name":"action","arguments":{"value":"allowed"}}""");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await host.DispatchAsync(invocation, request, denied, default));
        Assert.Equal(0, server.ToolCalls);

        var response = await host.DispatchAsync(invocation, request, ViewOptions(), default);

        Assert.Null(response.Error);
        Assert.Equal(1, server.ToolCalls);
        Assert.Equal("allowed", server.LastArguments!["value"]!.GetValue<string>());
    }

    [Fact]
    public async Task StandardToolSchemasAreForwardedWithoutHostSideInterpretation()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        var tools = CreateTools(client).ToArray();
        tools[1].ProtocolTool.InputSchema = JsonDocument.Parse("""
            {
                "$schema": "https://json-schema.org/draft/2020-12/schema",
                "type": "object",
                "$defs": { "value": { "type": "string" } },
                "properties": {
                    "value": { "$ref": "#/$defs/value" },
                    "count": { "type": "integer" }
                },
                "required": ["value", "count"],
                "not": { "required": ["denied"] },
                "dependentSchemas": {
                    "value": { "properties": { "count": { "type": "integer" } } }
                }
            }
            """).RootElement.Clone();
        host.BindTools(client, tools);
        var invocation = host.StartInvocation(host.FindAppTool("model_alias")!,
            new("call", "model_alias", new Dictionary<string, object?>()));

        var response = await host.DispatchAsync(invocation,
            Request("tools/call", """{"name":"action","arguments":{"value":"valid","count":1.0}}"""), ViewOptions(), default);

        Assert.Null(response.Error);
        Assert.Equal("first", response.Result!.Value.GetProperty("structuredContent").GetProperty("server").GetString());
        Assert.Equal(1, server.ToolCalls);
        Assert.Equal("action", server.LastToolName);
        Assert.Equal("valid", server.LastArguments!["value"]!.GetValue<string>());
        Assert.Equal(1m, JsonDocument.Parse(server.LastArguments["count"]!.ToJsonString()).RootElement.GetDecimal());
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
    public async Task MalformedParametersReturnInvalidParamsBeforeDispatch(string method, string json)
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = CreateHost();
        host.BindTools(client, CreateTools(client));
        var invocation = host.StartInvocation(host.FindAppTool("model_alias")!,
            new("call", "model_alias", new Dictionary<string, object?>()));

        var response = await host.DispatchAsync(invocation, Request(method, json), ViewOptions(), default);

        Assert.Null(response.Result);
        Assert.Equal(-32602, response.Error!.Code);
        Assert.Equal(0, server.ToolCalls);
    }

    [Fact]
    public async Task DuplicateCallAndRetentionLimitNeverExecuteAgain()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        using var host = new McpAppHost(new() { MaximumRetainedCalls = 1 });
        host.BindTools(client, CreateTools(client));
        var binding = host.FindAppTool("model_alias")!;
        host.StartInvocation(binding, new("same", "model_alias", new Dictionary<string, object?>()));

        Assert.Throws<InvalidOperationException>(() => host.StartInvocation(binding, new("same", "model_alias", new Dictionary<string, object?>())));
        Assert.Throws<InvalidOperationException>(() => host.StartInvocation(binding, new("different", "model_alias", new Dictionary<string, object?>())));
        Assert.Null(host.FindInvocation(new() { Call = new("restored", "model_alias", new Dictionary<string, object?>()) }));
        Assert.Equal(0, server.ToolCalls);
    }

    [Fact]
    public async Task DisposingHostCancelsRetainedWaitWithoutDisposingSharedClient()
    {
        var server = new ProtocolFixture("first");
        await using var client = await McpClient.CreateAsync(server);
        var host = CreateHost();
        host.BindTools(client, CreateTools(client));
        var binding = host.FindAppTool("model_alias")!;
        var invocation = host.StartInvocation(binding, new("pending", "model_alias", new Dictionary<string, object?>()));
        var pending = host.DispatchAsync(invocation, Request("$initialResult", "{}"), ViewOptions(), default).AsTask();

        host.Dispose();
        host.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await client.PingAsync();
        Assert.False(server.Disposed);
    }

    private static McpAppHost CreateHost() => new(new());

    private static McpContentOptions ViewOptions() => new() { ProxyUrl = new("https://proxy.example/proxy") };

    private static McpAppRequest Request(string method, string json) => new(method, JsonDocument.Parse(json).RootElement.Clone());

    private static IEnumerable<McpClientTool> CreateTools(McpClient client, string alias = "model_alias")
    {
        yield return new McpClientTool(client, Tool("open", """{"resourceUri":"ui://test/app"}""")).WithName(alias);
        yield return new McpClientTool(client, Tool("action", """{"visibility":["app"]}"""));
    }

    private static Tool Tool(string name, string ui) => new()
    {
        Name = name,
        InputSchema = JsonDocument.Parse("""{"type":"object","properties":{"value":{"type":"string"}},"required":["value"],"additionalProperties":false}""").RootElement.Clone(),
        Meta = new() { ["ui"] = JsonNode.Parse(ui) }
    };

    private sealed class ProtocolFixture(string serverName) : IClientTransport, ITransport
    {
        private readonly Channel<JsonRpcMessage> _messages = Channel.CreateUnbounded<JsonRpcMessage>();
        public string Name => serverName;
        public string? SessionId => null;
        public ChannelReader<JsonRpcMessage> MessageReader => _messages.Reader;
        public int DiscoveryRequests { get; private set; }
        public int InitializeRequests { get; private set; }
        public int ToolCalls { get; private set; }
        public int ResourceReads { get; private set; }
        public int ResourceLists { get; private set; }
        public string? LastToolName { get; private set; }
        public JsonNode? LastArguments { get; private set; }
        public bool Disposed { get; private set; }

        public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult<ITransport>(this);

        public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            if (message is not JsonRpcRequest request)
            {
                return;
            }
            JsonNode result;
            if (request.Method == "server/discover")
            {
                DiscoveryRequests++;
                result = new JsonObject
                {
                    ["supportedVersions"] = new JsonArray(request.Params!["_meta"]![MetaKeys.ProtocolVersion]!.DeepClone()),
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject(), ["resources"] = new JsonObject() },
                    ["ttlMs"] = 0,
                    ["cacheScope"] = "private",
                    ["_meta"] = new JsonObject
                    {
                        [MetaKeys.ServerInfo] = new JsonObject { ["name"] = serverName, ["version"] = "1" }
                    }
                };
            }
            else if (request.Method == "initialize")
            {
                InitializeRequests++;
                result = new JsonObject
                {
                    ["protocolVersion"] = request.Params!["protocolVersion"]!.DeepClone(),
                    ["capabilities"] = new JsonObject { ["tools"] = new JsonObject(), ["resources"] = new JsonObject() },
                    ["serverInfo"] = new JsonObject { ["name"] = serverName, ["version"] = "1" }
                };
            }
            else if (request.Method == "tools/call")
            {
                ToolCalls++;
                LastToolName = request.Params!["name"]!.GetValue<string>();
                LastArguments = request.Params["arguments"]?.DeepClone();
                result = new JsonObject
                {
                    ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = "Original result" }),
                    ["structuredContent"] = new JsonObject { ["server"] = serverName },
                    ["_meta"] = new JsonObject { ["ui"] = new JsonObject { ["private"] = "private-ui-data" }, ["trace"] = "business" }
                };
            }
            else if (request.Method == "resources/read")
            {
                ResourceReads++;
                result = new JsonObject
                {
                    ["contents"] = new JsonArray(new JsonObject
                    {
                        ["uri"] = request.Params!["uri"]!.DeepClone(),
                        ["mimeType"] = "text/plain",
                        ["text"] = serverName
                    })
                };
            }
            else if (request.Method == "resources/list")
            {
                ResourceLists++;
                result = new JsonObject
                {
                    ["resources"] = new JsonArray(
                        new JsonObject { ["uri"] = "ui://test/app", ["name"] = "App", ["mimeType"] = McpAppPolicy.HtmlMimeType },
                        new JsonObject { ["uri"] = "data://test/item", ["name"] = "Item", ["mimeType"] = "text/plain" })
                };
            }
            else if (request.Method == "ping")
            {
                result = new JsonObject();
            }
            else
            {
                throw new InvalidOperationException($"Unexpected protocol fixture method {request.Method}.");
            }
            await _messages.Writer.WriteAsync(new JsonRpcResponse { Id = request.Id, Result = result }, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            _messages.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ToolCallingClient(string toolName) : IChatClient
    {
        public int Requests { get; private set; }
        public FunctionCallContent Call { get; } = new("native-call", toolName, new Dictionary<string, object?> { ["value"] = "initial" });
        public FunctionResultContent? ToolResult { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Requests++;
            if (Requests == 1)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [Call])));
            }
            ToolResult = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Single();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetResponseAsync(messages, options, cancellationToken);
            foreach (var update in response.ToChatResponseUpdates())
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
