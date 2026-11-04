// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Binds discovered MCP tools to one conversation's isolated app views.</summary>
/// <remarks>The host does not own or dispose the supplied MCP connections.</remarks>
public sealed class McpAppHost : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<string, McpAppTool> _modelTools = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpAppInvocation> _invocations = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;

    /// <summary>Initializes a conversation-scoped app host.</summary>
    public McpAppHost(McpAppHostOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumRetainedCalls);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaximumResultBytes);
        if (options.OperationTimeout <= TimeSpan.Zero || options.OperationTimeout.TotalMilliseconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The originating call deadline must be a positive finite duration.");
        }
    }

    internal McpAppHostOptions Options { get; }

    /// <summary>Binds an SDK-discovered tool snapshot to its owning connection.</summary>
    /// <remarks>
    /// The caller must supply tools belonging to <paramref name="client"/>.
    /// Use SDK aliases to make model names unique.
    /// Apply existing model approval wrappers to the returned functions, not to the app broker.
    /// </remarks>
    /// <returns>The model-visible functions, decorated to retain original app results.</returns>
    public IReadOnlyList<AIFunction> BindTools(McpClient client, IEnumerable<McpClientTool> tools)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(tools);
        var connection = new McpAppConnection(client);
        var functions = new List<AIFunction>();
        var modelBindings = new Dictionary<string, McpAppTool>(StringComparer.Ordinal);
        foreach (var tool in tools)
        {
            var binding = new McpAppTool(connection, tool);
            connection.Tools.Add(tool.ProtocolTool.Name, binding);
            if (McpAppPolicy.VisibleTo(tool.ProtocolTool, "model"))
            {
                modelBindings.Add(tool.Name, binding);
                functions.Add(binding.ResourceUri is null ? tool : new McpAppFunction(this, binding));
            }
        }
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (modelBindings.Keys.Any(_modelTools.ContainsKey))
            {
                throw new ArgumentException("Model-facing tool names must be unique.");
            }
            foreach (var binding in modelBindings)
            {
                _modelTools.Add(binding.Key, binding.Value);
            }
        }
        return functions;
    }

    /// <summary>Registers the adapter's content handler using the existing agent mapping pipeline.</summary>
    public void Configure(UIAgentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddBlockHandler(new McpAppBlockHandler(this));
    }

    internal McpAppTool? FindAppTool(string name)
    {
        lock (_lock)
        {
            return _modelTools.TryGetValue(name, out var binding) && binding.ResourceUri is not null ? binding : null;
        }
    }

    internal McpAppInvocation StartInvocation(McpAppTool binding, FunctionCallContent call)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_invocations.ContainsKey(call.CallId))
            {
                throw new InvalidOperationException("This originating call identity has already executed; app restoration never replays it.");
            }
            if (_invocations.Count >= Options.MaximumRetainedCalls)
            {
                throw new InvalidOperationException("The conversation's retained app call limit was reached.");
            }
            var invocation = new McpAppInvocation(binding, call);
            _invocations.Add(call.CallId, invocation);
            return invocation;
        }
    }

    internal McpAppInvocation? FindInvocation(McpAppContentBlock block)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _invocations.TryGetValue(block.Id, out var invocation) &&
                invocation.Call.Name == block.ToolName ? invocation : null;
        }
    }

    internal CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken)
        => CreateOperationToken(cancellationToken, Options.OperationTimeout);

    private CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken, TimeSpan timeout)
    {
        var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        token.CancelAfter(timeout);
        return token;
    }

    internal void CheckResultSize(CallToolResult result)
    {
        if (Encoding.UTF8.GetByteCount(McpAppPolicy.Wire(result).GetRawText()) > Options.MaximumResultBytes)
        {
            throw new InvalidOperationException("The app tool result exceeds the configured byte limit.");
        }
    }

    internal async ValueTask<McpAppDocument> LoadDocumentAsync(McpAppInvocation invocation, McpContentOptions options,
        CancellationToken cancellationToken)
    {
        using var lifetime = CreateOperationToken(cancellationToken, options.OperationTimeout);
        lifetime.Token.ThrowIfCancellationRequested();
        var connection = invocation.Binding.Connection;
        var uri = invocation.Binding.ResourceUri!;
        Resource? listing = null;
        if (connection.Client.ServerCapabilities.Resources is not null)
        {
            var resources = await connection.Client.ListResourcesAsync(cancellationToken: lifetime.Token);
            listing = resources.FirstOrDefault(resource => resource.Uri == uri)?.ProtocolResource;
        }
        var result = await connection.Client.ReadResourceAsync(uri, cancellationToken: lifetime.Token);
        return McpAppPolicy.ReadDocument(result, uri, listing, options);
    }

    internal async ValueTask<McpAppResponse> DispatchAsync(McpAppInvocation invocation, McpAppRequest request,
        McpContentOptions options, CancellationToken cancellationToken)
    {
        try
        {
            return await DispatchCoreAsync(invocation, request, options, cancellationToken);
        }
        catch (JsonException error)
        {
            return new(Error: new(-32602, error.Message));
        }
    }

    private async ValueTask<McpAppResponse> DispatchCoreAsync(McpAppInvocation invocation, McpAppRequest request,
        McpContentOptions options, CancellationToken cancellationToken)
    {
        using var lifetime = CreateOperationToken(cancellationToken, options.OperationTimeout);
        lifetime.Token.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(request.Method))
        {
            throw new JsonException("An app request method is required.");
        }
        if (request.Parameters.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("App request parameters must be a JSON object.");
        }
        var connection = invocation.Binding.Connection;
        object result;
        switch (request.Method)
        {
            case "$initialResult":
                result = await invocation.Result.WaitAsync(lifetime.Token);
                break;
            case "tools/list":
                RejectCursor(request.Parameters);
                result = new JsonObject
                {
                    ["tools"] = new JsonArray(connection.Tools.Values
                        .Where(binding => CanCall(connection, binding.Tool.ProtocolTool, options))
                        .Select(binding => JsonNode.Parse(McpAppPolicy.Wire(binding.Tool.ProtocolTool).GetRawText())).ToArray())
                };
                break;
            case "tools/call":
                var name = RequiredString(request.Parameters, "name");
                if (!connection.Tools.TryGetValue(name, out var binding) || !CanCall(connection, binding.Tool.ProtocolTool, options))
                {
                    throw new InvalidOperationException("The requested app tool is not available on the owning connection.");
                }
                var arguments = request.Parameters.TryGetProperty("arguments", out var supplied)
                    ? supplied : JsonDocument.Parse("{}").RootElement.Clone();
                if (arguments.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException("App tool arguments must be a JSON object.");
                }
                var dictionary = arguments.EnumerateObject().ToDictionary(property => property.Name,
                    property => (object?)property.Value.Clone(), StringComparer.Ordinal);
                var raw = await binding.Tool.CallAsync(dictionary, cancellationToken: lifetime.Token);
                result = raw;
                break;
            case "resources/read":
                var uri = RequiredString(request.Parameters, "uri");
                if (uri != invocation.Binding.ResourceUri && options.AllowResourceRead?.Invoke(connection.Client, uri) is not true)
                {
                    throw new InvalidOperationException("The requested resource is not permitted by this host.");
                }
                result = await connection.Client.ReadResourceAsync(uri, cancellationToken: lifetime.Token);
                break;
            case "resources/list":
                RejectCursor(request.Parameters);
                var resources = await connection.Client.ListResourcesAsync(cancellationToken: lifetime.Token);
                result = new JsonObject
                {
                    ["resources"] = new JsonArray(resources
                        .Where(resource => resource.Uri == invocation.Binding.ResourceUri ||
                            options.AllowResourceRead?.Invoke(connection.Client, resource.Uri) is true)
                        .Select(resource => JsonNode.Parse(McpAppPolicy.Wire(resource.ProtocolResource).GetRawText())).ToArray())
                };
                break;
            default:
                return new(Error: new(-32601, $"Unsupported app operation: {request.Method}."));
        }
        return new(McpAppPolicy.Wire(result));
    }

    private static bool CanCall(McpAppConnection connection, Tool tool, McpContentOptions options) =>
        McpAppPolicy.VisibleTo(tool, "app") && options.AllowToolCall?.Invoke(connection.Client, tool.Name) is not false;

    private static string RequiredString(JsonElement parameters, string name)
    {
        if (parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
        {
            throw new JsonException($"A nonempty {name} is required.");
        }
        return value.GetString()!;
    }

    private static void RejectCursor(JsonElement parameters)
    {
        if (parameters.ValueKind == JsonValueKind.Object && parameters.TryGetProperty("cursor", out var cursor) &&
            cursor.ValueKind != JsonValueKind.Null)
        {
            throw new JsonException("The bound tool snapshot has no continuation cursor.");
        }
    }

    /// <summary>Cancels app operations and releases retained results without disposing shared MCP clients.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _lifetime.Cancel();
            foreach (var invocation in _invocations.Values)
            {
                invocation.Cancel();
            }
            _invocations.Clear();
            _modelTools.Clear();
        }
    }
}
