// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Extensions for rendering components.
    /// </summary>
    public static class HtmlHelperRazorComponentExtensions
    {
        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.RenderStaticComponentAsync<TComponent>(null);
        }

        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <param name="parameters">An <see cref="object"/> containing the parameters to pass
        /// to the component.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static async Task<IHtmlContent> RenderStaticComponentAsync<TComponent>(
            this IHtmlHelper htmlHelper,
            object parameters) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            var httpContext = htmlHelper.ViewContext.HttpContext;
            var serviceProvider = httpContext.RequestServices;
            var prerenderer = serviceProvider.GetRequiredService<StaticComponentRenderer>();

            var parametersCollection = parameters == null ?
                ParameterCollection.Empty :
                ParameterCollection.FromDictionary(HtmlHelper.ObjectToDictionary(parameters));

            var result = await prerenderer.PrerenderComponentAsync(
                parametersCollection,
                httpContext,
                typeof(TComponent));

            return new ComponentHtmlContent(result);
        }
    }
}
