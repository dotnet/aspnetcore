// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentResource(McpComponentApplication application, IServiceScopeFactory scopeFactory) : McpServerResource
{
    public override ResourceTemplate ProtocolResourceTemplate { get; } = new()
    {
        UriTemplate = application.ResourceUri,
        Name = $"{application.Name}_ui",
        MimeType = "text/html;profile=mcp-app",
    };

    public override IReadOnlyList<object> Metadata => [];

    public override bool IsMatch(string uri) => string.Equals(uri, application.ResourceUri, StringComparison.Ordinal);

    public override async ValueTask<ReadResourceResult> ReadAsync(RequestContext<ReadResourceRequestParams> request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = scopeFactory.CreateAsyncScope();
        var host = scope.ServiceProvider.GetRequiredService<McpComponentHost>();
        var rendered = await host.RenderAsync(application, new McpComponentRequest(
            application.InitialLocation, request.User ?? new ClaimsPrincipal(new ClaimsIdentity()), IsShellRendering: true), cancellationToken);
        if (rendered.Redirect is not null)
        {
            throw new McpException("The application resource shell must render a document without navigation.");
        }

        return new ReadResourceResult
        {
            Contents = [new TextResourceContents
            {
                Uri = application.ResourceUri,
                MimeType = "text/html;profile=mcp-app",
                Text = rendered.Html!,
                Meta = new JsonObject
                {
                    ["ui"] = new JsonObject
                    {
                        ["csp"] = new JsonObject
                        {
                            ["resourceDomains"] = new JsonArray(application.BaseUri.GetLeftPart(UriPartial.Authority)),
                            ["baseUriDomains"] = new JsonArray(application.BaseUri.GetLeftPart(UriPartial.Authority)),
                        },
                    },
                },
            }],
        };
    }
}
