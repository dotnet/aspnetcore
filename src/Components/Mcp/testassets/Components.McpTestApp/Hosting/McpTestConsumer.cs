// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Components.McpTestApp.Hosting;

public sealed class McpTestConsumer : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, Connection> _connections = new();

    public async Task<OpenedApp> OpenAsync(Uri producer, string hostOrigin, bool resultFirst, string openingToolName, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var client = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Endpoint = producer,
            TransportMode = HttpTransportMode.StreamableHttp,
        }), new McpClientOptions
        {
            ClientInfo = new Implementation { Name = "Components MCP test consumer", Version = "1.0.0" },
            Capabilities = new ClientCapabilities
            {
                Extensions = new Dictionary<string, object>
                {
                    ["io.modelcontextprotocol/ui"] = new JsonObject
                    {
                        ["mimeTypes"] = new JsonArray("text/html;profile=mcp-app"),
                    },
                },
            },
        }, cancellationToken: cancellationToken);
        var connection = new Connection(client, hostOrigin);
        if (!_connections.TryAdd(connection.Id, connection))
        {
            await client.DisposeAsync();
            throw new InvalidOperationException("The consumer connection identifier is already in use.");
        }

        try
        {
            await client.ListResourcesAsync(cancellationToken: cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            var tool = tools.Select(candidate => candidate.ProtocolTool).Single(candidate =>
                candidate.Name == openingToolName && candidate.Meta?["ui"]?["resourceUri"] is not null
                && IsVisible(candidate, "model"));
            connection.Tools = tools.Select(candidate => candidate.ProtocolTool).ToArray();
            var uri = tool.Meta!["ui"]!["resourceUri"]!.GetValue<string>();
            ReadResourceResult resourceResult;
            CallToolResult initialResult;
            if (resultFirst)
            {
                initialResult = await client.CallToolAsync(tool.Name, arguments, cancellationToken: cancellationToken);
                resourceResult = await client.ReadResourceAsync(uri, cancellationToken: cancellationToken);
            }
            else
            {
                resourceResult = await client.ReadResourceAsync(uri, cancellationToken: cancellationToken);
                initialResult = await client.CallToolAsync(tool.Name, arguments, cancellationToken: cancellationToken);
            }

            var resource = resourceResult.Contents.OfType<TextResourceContents>().Single(content =>
                content.Uri == uri && content.MimeType == "text/html;profile=mcp-app");
            return new(connection.Id, tool, resource, initialResult, arguments);
        }
        catch
        {
            _connections.TryRemove(connection.Id, out _);
            await client.DisposeAsync();
            throw;
        }
    }

    public Connection Get(Guid id)
        => _connections.TryGetValue(id, out var connection) ? connection : throw new InvalidOperationException("The consumer connection is not available.");

    public async Task<object> ForwardAsync(Guid id, BrokerRequest request, CancellationToken cancellationToken)
    {
        var connection = Get(id);
        return request.Method switch
        {
            "tools/list" => new ListToolsResult { Tools = connection.Tools.ToList() },
            "tools/call" => await CallAsync(connection, request.Parameters, cancellationToken),
            "resources/list" => new ListResourcesResult
            {
                Resources = (await connection.Client.ListResourcesAsync(cancellationToken: cancellationToken)).Select(resource => resource.ProtocolResource).ToList(),
            },
            "resources/read" => await connection.Client.ReadResourceAsync(request.Parameters.GetProperty("uri").GetString()!, cancellationToken: cancellationToken),
            _ => throw new InvalidOperationException($"The test consumer does not implement '{request.Method}'."),
        };
    }

    private static async Task<CallToolResult> CallAsync(Connection connection, JsonElement parameters, CancellationToken cancellationToken)
    {
        var name = parameters.GetProperty("name").GetString()!;
        var tool = connection.Tools.Single(tool => tool.Name == name);
        if (!IsVisible(tool, "app"))
        {
            throw new InvalidOperationException("The tool is not app-visible on the owning connection.");
        }

        var arguments = parameters.TryGetProperty("arguments", out var supplied)
            ? supplied.Deserialize<Dictionary<string, object?>>()!
            : new Dictionary<string, object?>();
        return await connection.Client.CallToolAsync(name, arguments, cancellationToken: cancellationToken);
    }

    private static bool IsVisible(Tool tool, string visibility)
        => tool.Meta?["ui"]?["visibility"] is not { } specified
            || specified.AsArray().Any(value => value?.GetValue<string>() == visibility);

    public async Task CloseAsync(Guid id)
    {
        if (_connections.TryRemove(id, out var connection))
        {
            await connection.Client.DisposeAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var id in _connections.Keys)
        {
            await CloseAsync(id);
        }
    }

    public sealed record OpenedApp(Guid Id, Tool Tool, TextResourceContents Resource, CallToolResult Result, Dictionary<string, object?> Arguments);
    public sealed record BrokerRequest(string Method, JsonElement Parameters);

    public sealed class Connection(McpClient client, string hostOrigin)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public McpClient Client { get; } = client;
        public string HostOrigin { get; } = hostOrigin;
        public Tool[] Tools { get; set; } = [];
    }
}
