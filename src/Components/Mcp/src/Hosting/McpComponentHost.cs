// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Mcp.FormMapping;
using Microsoft.AspNetCore.Components.Mcp.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Components.Mcp.Protocol;

namespace Microsoft.AspNetCore.Components.Mcp.Hosting;

internal sealed class McpComponentHost
{
    private readonly IServiceProvider _services;

    public McpComponentHost(IServiceProvider services)
    {
        _services = services;
    }

    public Task<McpRenderResult> RenderAsync(
        McpComponentApplication application,
        McpComponentRequest request,
        CancellationToken cancellationToken = default)
        => RenderAsync(application, request, McpComponentParameters.Empty, cancellationToken);

    public async Task<McpRenderResult> RenderAsync(
        McpComponentApplication application,
        McpComponentRequest request,
        McpComponentParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.User);
        if (!request.Location.IsAbsoluteUri || !application.BaseUri.IsBaseOf(request.Location))
        {
            throw new ArgumentException("The component location must be within the registered application base URI.", nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var services = _services;
        var context = services.GetRequiredService<McpOperationContext>();
        context.Initialize(request, application.BaseUri, cancellationToken);
        if (request.HandlerName is { } handler)
        {
            services.GetRequiredService<McpFormDataProvider>().Initialize(
                handler,
                request.Form ?? new Dictionary<string, StringValues>(),
                request.Files ?? new FormFileCollection());
        }

        await using var renderer = new McpHtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        if (services.GetRequiredService<NavigationManager>() is not McpNavigationManager)
        {
            throw new InvalidOperationException("The MCP component navigation service was replaced. Register WithRazorComponents after other component hosting services, including AddInteractiveServerComponents.");
        }

        try
        {
            return await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var root = renderer.BeginRenderingComponent(application.ComponentType, ParameterView.FromDictionary(parameters.Restore()));
                await root.QuiescenceTask;
                cancellationToken.ThrowIfCancellationRequested();
                if (context.Redirect is null && request.HandlerName is { } handlerName)
                {
                    await renderer.DispatchSubmitAsync(handlerName);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return context.Redirect is { } redirect
                    ? McpRenderResult.ForRedirect(redirect, context.ReplaceHistory)
                    : McpRenderResult.ForDocument(root.ToHtmlString());
            });
        }
        catch (NavigationException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return McpRenderResult.ForRedirect(new Uri(exception.Location), context.ReplaceHistory);
        }
    }
}

internal sealed record McpRenderResult(string? Html, Uri? Redirect, bool ReplaceHistory)
{
    public static McpRenderResult ForDocument(string html) => new(html, null, false);
    public static McpRenderResult ForRedirect(Uri location, bool replaceHistory) => new(null, location, replaceHistory);
}
