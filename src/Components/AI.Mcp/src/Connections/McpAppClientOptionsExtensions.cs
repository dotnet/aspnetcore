// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Configures SDK connections to negotiate support for hosted MCP Apps.</summary>
public static class McpAppClientOptionsExtensions
{
    /// <summary>Advertises HTML MCP Apps support before an SDK client connects.</summary>
    /// <param name="options">The options used to create the SDK client.</param>
    /// <remarks>
    /// Other client capabilities and extensions are preserved. The UI extension entry is replaced
    /// with the capabilities supported by this host. An already-connected client cannot renegotiate.
    /// </remarks>
    /// <example>
    /// <code>
    /// var options = new McpClientOptions();
    /// options.EnableMcpApps();
    /// var client = await McpClient.CreateAsync(transport, options);
    /// </code>
    /// </example>
    public static void EnableMcpApps(this McpClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Capabilities ??= new ClientCapabilities();
        options.Capabilities.Extensions ??= new Dictionary<string, object>(StringComparer.Ordinal);
        options.Capabilities.Extensions["io.modelcontextprotocol/ui"] = new JsonObject
        {
            ["mimeTypes"] = new JsonArray(McpAppPolicy.HtmlMimeType),
        };
    }
}
