// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

internal static class McpAppProxy
{
    internal static string CreateDocument(Uri hostOrigin, Uri proxyOrigin, McpContentOptions? options)
    {
        if (!hostOrigin.IsAbsoluteUri || hostOrigin.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("The MCP proxy requires an absolute HTTP host origin.", nameof(hostOrigin));
        }
        options ??= new() { ProxyUrl = proxyOrigin };
        options.Validate();
        var origin = JsonSerializer.Serialize(hostOrigin.GetLeftPart(UriPartial.Authority));
        var module = JsonSerializer.Serialize(new Uri(proxyOrigin,
            "/_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-proxy.js").AbsoluteUri);
        return $$"""
            <!doctype html><html><head><meta charset="utf-8"><style>html,body,iframe{margin:0;border:0;width:100%;height:100%}</style></head>
            <body><script type="module">import { start } from {{module}}; await start({{origin}}, {maximumMessageBytes:{{options.MaximumMessageBytes}}});</script></body></html>
            """;
    }
}
