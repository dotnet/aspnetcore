// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Rendering;

/// <summary>
/// Extensions for rendering components.
/// </summary>
public static class HtmlHelperComponentExtensions
{
    /// <summary>
    /// Renders the <typeparamref name="TComponent"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
    /// <param name="renderMode">The <see cref="RenderMode"/> for the component.</param>
    /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
    public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper, RenderMode renderMode) where TComponent : IComponent
        => RenderComponentAsync<TComponent>(htmlHelper, renderMode, parameters: null);

    /// <summary>
    /// Renders the <typeparamref name="TComponent"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
    /// <param name="parameters">An <see cref="object"/> containing the parameters to pass
    /// to the component.</param>
    /// <param name="renderMode">The <see cref="RenderMode"/> for the component.</param>
    /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
    public static Task<IHtmlContent> RenderComponentAsync<TComponent>(
        this IHtmlHelper htmlHelper,
        RenderMode renderMode,
        object parameters) where TComponent : IComponent
        => RenderComponentAsync(htmlHelper, typeof(TComponent), renderMode, parameters);

    /// <summary>
    /// Renders the specified <paramref name="componentType"/>.
    /// </summary>
    /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
    /// <param name="componentType">The component type.</param>
    /// <param name="parameters">An <see cref="object"/> containing the parameters to pass
    /// to the component.</param>
    /// <param name="renderMode">The <see cref="RenderMode"/> for the component.</param>
    public static async Task<IHtmlContent> RenderComponentAsync(
        this IHtmlHelper htmlHelper,
        Type componentType,
        RenderMode renderMode,
        object parameters)
    {
        ArgumentNullException.ThrowIfNull(htmlHelper);
        ArgumentNullException.ThrowIfNull(componentType);

        var parameterView = parameters is null ?
            ParameterView.Empty :
            ParameterView.FromDictionary(HtmlHelper.ObjectToDictionary(parameters));

        var httpContext = htmlHelper.ViewContext.HttpContext;
        var componentRenderer = httpContext.RequestServices.GetRequiredService<IComponentPrerenderer>();
        var asyncContent = await componentRenderer.PrerenderComponentAsync(httpContext, componentType, MapRenderMode(renderMode), parameterView);

        // For back-compat, we have to return an IHtmlContent, which unfortunately means we have to buffer the content here.
        // That's not a regression - we've been doing that since Html.RenderComponentAsync was created. Fortunately the same
        // problem does not occur with the <component> tag helper or the newer .NET 8 methods for server-side rendering of
        // components, since they can use IHtmlAsyncContent.
        var viewBufferScope = httpContext.RequestServices.GetRequiredService<IViewBufferScope>();
        var viewBuffer = new ViewBuffer(viewBufferScope, nameof(RenderComponentAsync), ViewBuffer.ViewPageSize);
        var viewContextWriter = htmlHelper.ViewContext.Writer;
        var htmlEncoder = httpContext.RequestServices.GetRequiredService<HtmlEncoder>();
        using var writer = new ViewBufferTextWriter(viewBuffer, viewContextWriter.Encoding, htmlEncoder, viewContextWriter);
        await asyncContent.WriteToAsync(writer);
        return viewBuffer;
    }

    // This is unfortunate, and we might want to find a better solution. There's an existing public
    // type Microsoft.AspNetCore.Mvc.Rendering.RenderMode which we now need to use in a lower layer,
    // M.A.C.Endpoints. Even type-forwarding is not a good solution because we really want to change
    // the namespace. So this code maps the old enum to the newer one.
    internal static Components.RenderMode MapRenderMode(RenderMode renderMode) => renderMode switch
    {
        RenderMode.Static => Components.RenderMode.Static,
        RenderMode.Server => Components.RenderMode.Server,
        RenderMode.ServerPrerendered => Components.RenderMode.ServerPrerendered,
        RenderMode.WebAssembly => Components.RenderMode.WebAssembly,
        RenderMode.WebAssemblyPrerendered => Components.RenderMode.WebAssemblyPrerendered,
        _ => throw new ArgumentException($"Unsupported render mode {renderMode}", nameof(renderMode)),
    };
}
