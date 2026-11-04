// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;
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

    // The tag helper uses a simple enum to represent render mode, whereas Blazor internally has a richer
    // object-based way to represent render modes. This converts from tag helper enum values to the
    // object representation.
    internal static IComponentRenderMode MapRenderMode(RenderMode renderMode) => renderMode switch
    {
        RenderMode.Static => null,
        RenderMode.Server => new InteractiveServerRenderMode(prerender: false),
        RenderMode.ServerPrerendered => Components.Web.RenderMode.InteractiveServer,
        RenderMode.WebAssembly => new InteractiveWebAssemblyRenderMode(prerender: false),
        RenderMode.WebAssemblyPrerendered => Components.Web.RenderMode.InteractiveWebAssembly,
        _ => throw new ArgumentException($"Unsupported render mode {renderMode}", nameof(renderMode)),
    };
}
