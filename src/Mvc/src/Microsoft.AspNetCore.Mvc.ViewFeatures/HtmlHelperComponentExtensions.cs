// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Environment;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Extensions for rendering components.
    /// </summary>
    public static class HtmlHelperComponentExtensions
    {
        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.RenderComponentAsync<TComponent>(null);
        }

        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <param name="parameters">An <see cref="object"/> containing the parameters to pass
        /// to the component.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static async Task<IHtmlContent> RenderComponentAsync<TComponent>(
            this IHtmlHelper htmlHelper,
            object parameters) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            var httpContext = htmlHelper.ViewContext.HttpContext;
            var serviceProvider = httpContext.RequestServices;
            var prerrenderContext = serviceProvider.GetRequiredService<MvcPrerrenderingContext>();
            var encoder = prerrenderContext.Encoder;
            prerrenderContext.Environment.Name = ComponentEnvironment.Prerrender;
            // Add our custom JSRuntime that throws upon invocation. We might need an option here to not throw or log a warning
            // message instead.
            prerrenderContext.Environment.JSRuntime = new UnsupportedJavaScriptRuntime();
            prerrenderContext.Environment.UriHelper = new HttpUriHelper(httpContext);

            var dispatcher = Renderer.CreateDefaultDispatcher();
            using (var htmlRenderer = new HtmlRenderer(serviceProvider, encoder.Encode, dispatcher))
            {
                var result = await dispatcher.InvokeAsync(() => htmlRenderer.RenderComponentAsync<TComponent>(
                    parameters == null ?
                        ParameterCollection.Empty :
                        ParameterCollection.FromDictionary(HtmlHelper.ObjectToDictionary(parameters))));
                return new ComponentHtmlContent(result);
            }
        }
    }
}
