// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
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
        if (htmlHelper is null)
        {
            throw new ArgumentNullException(nameof(htmlHelper));
        }

        if (componentType is null)
        {
            throw new ArgumentNullException(nameof(componentType));
        }

        var viewContext = htmlHelper.ViewContext;
        var componentRenderer = viewContext.HttpContext.RequestServices.GetRequiredService<IComponentRenderer>();
        return await componentRenderer.RenderComponentAsync(viewContext, componentType, renderMode, parameters);
    }
}
