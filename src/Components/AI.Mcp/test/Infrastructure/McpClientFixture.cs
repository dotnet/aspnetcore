// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

internal sealed class McpClientFixture(string serverName) : IClientTransport, ITransport
{
    internal const string HtmlMimeType = "text/html;profile=mcp-app";
    private readonly Channel<JsonRpcMessage> _messages = Channel.CreateUnbounded<JsonRpcMessage>();
    public string Name => serverName;
    public string? SessionId => null;
    public ChannelReader<JsonRpcMessage> MessageReader => _messages.Reader;
    public int ToolCalls { get; private set; }
    public int ToolLists { get; private set; }
    public int ResourceReads { get; private set; }
    public int ResourceLists { get; private set; }
    public string? LastToolName { get; private set; }
    public JsonNode? LastArguments { get; private set; }
    public bool Disposed { get; private set; }
    public IReadOnlyList<Tool> ToolDefinitions { get; set; } =
    [
        Tool("open", """{"resourceUri":"ui://test/app"}"""),
        Tool("action", """{"visibility":["app"]}""")
    ];
    public ReadResourceResult? ResourceResult { get; set; }
    public IList<Resource> Resources { get; set; } =
    [
        new() { Uri = "ui://test/app", Name = "App", MimeType = HtmlMimeType },
        new() { Uri = "data://test/item", Name = "Item", MimeType = "text/plain" }
    ];
    public CallToolResult Result { get; set; } = new()
    {
        Content = [new TextContentBlock { Text = "Original result" }],
        StructuredContent = JsonSerializer.SerializeToElement(new { server = serverName }),
        Meta = new() { ["viewOnly"] = "Not for the model." }
    };

    public static Tool Tool(string name, string ui) => new()
    {
        Name = name,
        InputSchema = JsonDocument.Parse("""{"type":"object","properties":{"value":{"type":"string"}}}""").RootElement.Clone(),
        Meta = new() { ["ui"] = JsonNode.Parse(ui) }
    };

    public Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult<ITransport>(this);

    public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (message is not JsonRpcRequest request)
        {
            return;
        }
        JsonNode result;
        switch (request.Method)
        {
            case "server/discover":
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
                break;
            case "tools/list":
                ToolLists++;
                result = new JsonObject
                {
                    ["tools"] = JsonSerializer.SerializeToNode(ToolDefinitions, McpJsonUtilities.DefaultOptions)
                };
                break;
            case "tools/call":
                ToolCalls++;
                LastToolName = request.Params!["name"]!.GetValue<string>();
                LastArguments = request.Params["arguments"]?.DeepClone();
                result = JsonSerializer.SerializeToNode(Result, McpJsonUtilities.DefaultOptions)!;
                break;
            case "resources/read":
                ResourceReads++;
                if (ResourceResult is not null)
                {
                    result = JsonSerializer.SerializeToNode(ResourceResult, McpJsonUtilities.DefaultOptions)!;
                    break;
                }
                result = new JsonObject
                {
                    ["contents"] = new JsonArray(new JsonObject
                    {
                        ["uri"] = request.Params!["uri"]!.DeepClone(),
                        ["mimeType"] = request.Params["uri"]!.GetValue<string>() == "ui://test/app"
                            ? HtmlMimeType : "text/plain",
                        ["text"] = "<html><body>App</body></html>"
                    })
                };
                break;
            case "resources/list":
                ResourceLists++;
                result = JsonSerializer.SerializeToNode(new ListResourcesResult { Resources = Resources },
                    McpJsonUtilities.DefaultOptions)!;
                break;
            case "ping":
                result = new JsonObject();
                break;
            default:
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
