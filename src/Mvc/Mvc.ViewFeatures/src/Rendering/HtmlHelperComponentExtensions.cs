// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
        return await componentRenderer.PrerenderComponentAsync(httpContext, componentType, MapRenderMode(renderMode), parameterView);
    }

    // This is unfortunate, and we might want to find a better solution. There's an existing public
    // type Microsoft.AspNetCore.Mvc.Rendering.RenderMode which we now need to use in a lower layer,
    // M.A.C.Endpoints. Even type-forwarding is not a good solution because we really want to change
    // the namespace. So this code maps the old enum to the newer one.
    private static Components.RenderMode MapRenderMode(RenderMode renderMode) => renderMode switch
    {
        RenderMode.Static => Components.RenderMode.Static,
        RenderMode.Server => Components.RenderMode.Server,
        RenderMode.ServerPrerendered => Components.RenderMode.ServerPrerendered,
        RenderMode.WebAssembly => Components.RenderMode.WebAssembly,
        RenderMode.WebAssemblyPrerendered => Components.RenderMode.WebAssemblyPrerendered,
        _ => throw new ArgumentException($"Unsupported render mode {renderMode}", nameof(renderMode)),
    };
}
