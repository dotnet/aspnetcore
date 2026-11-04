// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.Text.Json.Nodes;

namespace McpAppsServer;

[McpServerResourceType]
public sealed class NoteResource(NoteStore store)
{
    public const string ResourceUri = "ui://note/app.html";
    private const string HtmlMimeType = "text/html;profile=mcp-app";

    [McpServerResource(UriTemplate = ResourceUri, Name = "note-app", MimeType = HtmlMimeType)]
    public ReadResourceResult Read()
    {
        using var stream = typeof(NoteResource).Assembly.GetManifestResourceStream("McpAppsServer.standard-app.js")
            ?? throw new InvalidOperationException("The standard app bundle was not built.");
        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd().Replace("</script", "<\\/script", StringComparison.OrdinalIgnoreCase);
        var origin = store.Origin.GetLeftPart(UriPartial.Authority);

        return new()
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = ResourceUri,
                    MimeType = HtmlMimeType,
                    Text = $$"""
                        <!doctype html>
                        <html lang="en"><head><meta charset="utf-8"><title>Standard note</title></head>
                        <body>
                        <h1 id="title">Loading</h1>
                        <p id="message">Waiting for the original tool result.</p>
                        <p id="metadata"></p><p id="theme"></p><p id="arguments"></p>
                        <label>Draft <input id="draft"></label><button id="save">Save</button>
                        <button id="model-only">Call model-only tool</button>
                        <button id="unknown">Call unknown tool</button>
                        <button id="cross-server">Call another server tool</button>
                        <button id="load-asset">Load allowed asset</button><img id="allowed-asset" alt="Approved asset">
                        <button id="connect-socket">Use allowed network origin</button><p id="socket-result"></p>
                        <button id="slow">Start slow call</button><button id="cancel">Cancel slow call</button>
                        <button id="transport-failure">Wait for server failure</button>
                        <p id="status" role="status"></p><p id="error" role="alert"></p>
                        <script type="module">{{script}}</script>
                        </body></html>
                        """,
                    Meta = new JsonObject
                    {
                        ["ui"] = new JsonObject
                        {
                            ["csp"] = new JsonObject
                            {
                                ["resourceDomains"] = new JsonArray(origin),
                                ["connectDomains"] = new JsonArray(store.SocketOrigin),
                            },
                        },
                    },
                },
            ],
        };
    }
}
