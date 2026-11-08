// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.AI.Mcp;

/// <summary>Renders a complete static MCP Apps sandbox document.</summary>
/// <remarks>
/// Derive a Razor component from this type to customize the document body.
/// The body must contain exactly one iframe with the ID <c>mcp-app</c>.
/// Map this type directly to use its default iframe.
/// </remarks>
public class McpSandboxBase : IComponent
{
    private RenderHandle _renderHandle;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Gets or sets the parent host origin supplied by the sandbox endpoint.</summary>
    [Parameter, EditorRequired]
    public string HostOrigin { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        if (_renderHandle.IsInitialized)
        {
            throw new InvalidOperationException("The sandbox component is already attached to a renderer.");
        }
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        _renderHandle.Render(builder =>
        {
            builder.AddMarkupContent(0, "<!doctype html>");
            builder.OpenElement(1, "html");
            builder.OpenElement(2, "head");
            builder.OpenElement(3, "meta");
            builder.AddAttribute(4, "charset", "utf-8");
            builder.CloseElement();
            builder.OpenElement(5, "title");
            builder.AddContent(6, "MCP app sandbox");
            builder.CloseElement();
            builder.OpenElement(7, "style");
            builder.AddContent(8, "html,body,iframe{margin:0;border:0;width:100%;height:100%}iframe{display:block}");
            builder.CloseElement();
            builder.CloseElement();
            builder.OpenElement(9, "body");
            builder.OpenRegion(10);
            BuildRenderTree(builder);
            builder.CloseRegion();
            builder.OpenElement(11, "script");
            builder.AddAttribute(12, "type", "module");
            builder.AddMarkupContent(13,
                $"import {{ start }} from {JsonSerializer.Serialize(Navigation.ToAbsoluteUri("_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js").AbsoluteUri)};" +
                $"await start(document.getElementById('mcp-app'),{JsonSerializer.Serialize(HostOrigin)});");
            builder.CloseElement();
            builder.CloseElement();
            builder.CloseElement();
        });
        return Task.CompletedTask;
    }

    /// <summary>Renders the sandbox body containing the <c>mcp-app</c> iframe.</summary>
    /// <param name="builder">The render-tree builder for the document body.</param>
    protected virtual void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "iframe");
        builder.AddAttribute(1, "id", "mcp-app");
        builder.AddAttribute(2, "title", "MCP app document");
        builder.AddAttribute(3, "sandbox", "allow-scripts");
        builder.AddAttribute(4, "referrerpolicy", "no-referrer");
        builder.CloseElement();
    }
}
