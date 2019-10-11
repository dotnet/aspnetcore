// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
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
        public static Task<IHtmlContent> RenderComponentAsync(
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
            return componentRenderer.RenderComponentAsync(viewContext, componentType, renderMode, parameters);
        }
    }
}
