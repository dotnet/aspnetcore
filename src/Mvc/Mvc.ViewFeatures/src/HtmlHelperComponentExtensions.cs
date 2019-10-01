// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.RazorComponents;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Extensions for rendering components.
    /// </summary>
    public static class HtmlHelperComponentExtensions
    {
        private static readonly object ComponentSequenceKey = new object();

        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <param name="renderMode">The <see cref="RenderMode"/> for the component.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static Task<IHtmlContent> RenderComponentAsync<TComponent>(this IHtmlHelper htmlHelper, RenderMode renderMode) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            return htmlHelper.RenderComponentAsync<TComponent>(renderMode, null);
        }

        /// <summary>
        /// Renders the <typeparamref name="TComponent"/> <see cref="IComponent"/>.
        /// </summary>
        /// <param name="htmlHelper">The <see cref="IHtmlHelper"/>.</param>
        /// <param name="parameters">An <see cref="object"/> containing the parameters to pass
        /// to the component.</param>
        /// <param name="renderMode">The <see cref="RenderMode"/> for the component.</param>
        /// <returns>The HTML produced by the rendered <typeparamref name="TComponent"/>.</returns>
        public static async Task<IHtmlContent> RenderComponentAsync<TComponent>(
            this IHtmlHelper htmlHelper,
            RenderMode renderMode,
            object parameters) where TComponent : IComponent
        {
            if (htmlHelper == null)
            {
                throw new ArgumentNullException(nameof(htmlHelper));
            }

            var context = htmlHelper.ViewContext.HttpContext;
            return renderMode switch
            {
                RenderMode.Server => NonPrerenderedServerComponent(context, GetOrCreateInvocationId(htmlHelper.ViewContext), typeof(TComponent), GetParametersCollection(parameters)),
                RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(context, GetOrCreateInvocationId(htmlHelper.ViewContext), typeof(TComponent), GetParametersCollection(parameters)),
                RenderMode.Static => await StaticComponentAsync(context, typeof(TComponent), GetParametersCollection(parameters)),
                _ => throw new ArgumentException("Invalid render mode", nameof(renderMode)),
            };
        }

        private static ServerComponentInvocationSequence GetOrCreateInvocationId(ViewContext viewContext)
        {
            if (!viewContext.Items.TryGetValue(ComponentSequenceKey, out var result))
            {
                result = new ServerComponentInvocationSequence();
                viewContext.Items[ComponentSequenceKey] = result;
            }

            return (ServerComponentInvocationSequence)result;
        }

        private static ParameterView GetParametersCollection(object parameters) => parameters == null ?
                ParameterView.Empty :
                ParameterView.FromDictionary(HtmlHelper.ObjectToDictionary(parameters));

        private static async Task<IHtmlContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
        {
            var serviceProvider = context.RequestServices;
            var prerenderer = serviceProvider.GetRequiredService<StaticComponentRenderer>();


            var result = await prerenderer.PrerenderComponentAsync(
                parametersCollection,
                context,
                type);

            return new ComponentHtmlContent(result);
        }

        private static async Task<IHtmlContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
        {
            if (parametersCollection.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException("Prerendering server components with parameters is not supported.");
            }

            var serviceProvider = context.RequestServices;
            var prerenderer = serviceProvider.GetRequiredService<StaticComponentRenderer>();
            var invocationSerializer = serviceProvider.GetRequiredService<ServerComponentSerializer>();

            var currentInvocation = invocationSerializer.SerializeInvocation(
                invocationId,
                type,
                prerendered: true);

            var result = await prerenderer.PrerenderComponentAsync(
                parametersCollection,
                context,
                type);

            return new ComponentHtmlContent(
                invocationSerializer.GetPreamble(currentInvocation),
                result,
                invocationSerializer.GetEpilogue(currentInvocation));
        }

        private static IHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
        {
            if (parametersCollection.GetEnumerator().MoveNext())
            {
                throw new InvalidOperationException("Server components with parameters are not supported.");
            }

            var serviceProvider = context.RequestServices;
            var invocationSerializer = serviceProvider.GetRequiredService<ServerComponentSerializer>();
            var currentInvocation = invocationSerializer.SerializeInvocation(invocationId, type, prerendered: false);

            return new ComponentHtmlContent(invocationSerializer.GetPreamble(currentInvocation));
        }
    }
}
