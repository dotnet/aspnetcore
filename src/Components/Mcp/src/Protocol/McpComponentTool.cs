// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentTool : McpServerTool
{
    private readonly McpComponentApplication _application;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly bool _dispatch;

    public McpComponentTool(McpComponentApplication application, IServiceScopeFactory scopeFactory, bool dispatch)
    {
        _application = application;
        _scopeFactory = scopeFactory;
        _dispatch = dispatch;
        ProtocolTool = new Tool
        {
            Name = dispatch ? application.DispatchToolName : application.Name,
            Description = dispatch ? "Submits or navigates the component application." : "Opens the component application.",
            InputSchema = JsonSerializer.SerializeToElement(dispatch
                ? new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["viewId"] = new JsonObject { ["type"] = "string" },
                        ["location"] = new JsonObject { ["type"] = "string" },
                        ["method"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("GET", "POST", "CLOSE") },
                        ["enctype"] = new JsonObject { ["type"] = "string" },
                        ["fields"] = new JsonObject { ["type"] = "object" },
                        ["files"] = new JsonObject { ["type"] = "array" },
                    },
                    ["required"] = new JsonArray("viewId", "method"),
                    ["additionalProperties"] = false,
                }
                : new JsonObject { ["type"] = "object", ["properties"] = new JsonObject(), ["additionalProperties"] = false }),
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["resourceUri"] = application.ResourceUri,
                    ["visibility"] = dispatch ? new JsonArray("app") : new JsonArray("model", "app"),
                },
            },
        };
    }

    public override Tool ProtocolTool { get; }
    public override IReadOnlyList<object> Metadata => [];

    public override async ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams> request, CancellationToken cancellationToken = default)
    {
        if (!_dispatch)
        {
            await using var openingScope = _scopeFactory.CreateAsyncScope();
            var openingInvoker = openingScope.ServiceProvider.GetRequiredKeyedService<McpComponentInvoker>(_application);
            return await openingInvoker.OpenFromToolAsync(request, openingResult: null, cancellationToken);
        }

        var arguments = request.Params?.Arguments ?? throw new McpException("Component dispatch arguments are required.");
        if (!arguments.TryGetValue("viewId", out var idElement) || idElement.ValueKind != JsonValueKind.String
            || !Guid.TryParse(idElement.GetString(), out var id)
            || !arguments.TryGetValue("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
        {
            throw new McpException("Component dispatch requires a view identifier and method.");
        }

        var view = _application.GetView(id, request.User ?? new ClaimsPrincipal(new ClaimsIdentity()));
        return methodElement.GetString() == "CLOSE"
            ? await DispatchAsync(view, request, cancellationToken)
            : await view.RunAsync(token => DispatchAsync(view, request, token), cancellationToken);
    }

    private async Task<CallToolResult> DispatchAsync(
        McpComponentApplication.View view,
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var invoker = scope.ServiceProvider.GetRequiredKeyedService<McpComponentInvoker>(_application);
        return await invoker.DispatchAsync(view, request, cancellationToken);
    }
}
