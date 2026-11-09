// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentOptionsSetup(
    McpComponentApplication application,
    IEnumerable<McpServerTool> registeredTools,
    IEnumerable<McpServerResource> registeredResources,
    IServiceScopeFactory scopeFactory) : IPostConfigureOptions<McpServerOptions>
{
    public void PostConfigure(string? name, McpServerOptions options)
    {
        var openingTool = GetUniqueTool(options, application.OpeningToolName);
        GetUniqueTool(options, application.DispatchToolName);
        var resources = registeredResources.Where(resource => resource.ProtocolResourceTemplate.UriTemplate == application.ResourceUri)
            .Distinct(ReferenceEqualityComparer.Instance).Count();
        if (resources != 1)
        {
            throw new InvalidOperationException($"The component resource '{application.ResourceUri}' is registered more than once.");
        }

        var metadata = openingTool.ProtocolTool.Meta?.DeepClone().AsObject() ?? new JsonObject();
        var ui = metadata["ui"] is null ? new JsonObject() : metadata["ui"]!.AsObject();
        if (ui["resourceUri"] is { } existing && existing.GetValue<string>() != application.ResourceUri)
        {
            throw new InvalidOperationException($"The tool '{application.OpeningToolName}' is already associated with another app resource.");
        }

        ui["resourceUri"] = application.ResourceUri;
        if (metadata["ui"] is null)
        {
            metadata["ui"] = ui;
        }

        openingTool.ProtocolTool.Meta = metadata;
        if (application.InitialParametersFactory is not null)
        {
            options.Filters.Request.CallToolFilters.Insert(0, next => async (request, cancellationToken) =>
            {
                var result = await next(request, cancellationToken);
                if (request.Params?.Name != application.OpeningToolName || result.IsError == true)
                {
                    return result;
                }

                cancellationToken.ThrowIfCancellationRequested();
                await using var scope = scopeFactory.CreateAsyncScope();
                var invoker = scope.ServiceProvider.GetRequiredKeyedService<McpComponentInvoker>(application);
                return await invoker.OpenFromToolAsync(request, result, cancellationToken);
            });
        }
    }

    private McpServerTool GetUniqueTool(McpServerOptions options, string toolName)
    {
        var published = options.ToolCollection?.Where(tool => tool.ProtocolTool.Name == toolName).ToArray() ?? [];
        if (published.Length != 1)
        {
            throw new InvalidOperationException($"The component tool '{toolName}' must be registered exactly once in the MCP SDK tool catalog.");
        }

        var candidates = new HashSet<McpServerTool>(
            registeredTools.Where(tool => tool.ProtocolTool.Name == toolName), ReferenceEqualityComparer.Instance);
        candidates.Add(published[0]);
        if (candidates.Count != 1)
        {
            throw new InvalidOperationException($"Multiple MCP tools are registered with the component tool name '{toolName}'.");
        }

        return published[0];
    }
}
