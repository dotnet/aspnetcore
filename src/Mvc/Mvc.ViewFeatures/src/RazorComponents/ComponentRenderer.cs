// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class ComponentRenderer : IComponentRenderer
    {
        private static readonly object ComponentSequenceKey = new object();
        private readonly StaticComponentRenderer _staticComponentRenderer;
        private readonly ServerComponentSerializer _serverComponentSerializer;

        public ComponentRenderer(
            StaticComponentRenderer staticComponentRenderer,
            ServerComponentSerializer serverComponentSerializer)
        {
            _staticComponentRenderer = staticComponentRenderer;
            _serverComponentSerializer = serverComponentSerializer;
        }

        public async Task<IHtmlContent> RenderComponentAsync(
            ViewContext viewContext,
            Type componentType,
            RenderMode renderMode,
            object parameters)
        {
            if (viewContext is null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (componentType is null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException(Resources.FormatTypeMustDeriveFromType(componentType, typeof(IComponent)));
            }

            var context = viewContext.HttpContext;
            var parameterView = parameters is null ?
                ParameterView.Empty :
                ParameterView.FromDictionary(HtmlHelper.ObjectToDictionary(parameters));

            return renderMode switch
            {
                RenderMode.Server => NonPrerenderedServerComponent(context, GetOrCreateInvocationId(viewContext), componentType, parameterView),
                RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(context, GetOrCreateInvocationId(viewContext), componentType, parameterView),
                RenderMode.Static => await StaticComponentAsync(context, componentType, parameterView),
                _ => throw new ArgumentException(Resources.FormatUnsupportedRenderMode(renderMode), nameof(renderMode)),
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

        private async Task<IHtmlContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
        {
            var result = await _staticComponentRenderer.PrerenderComponentAsync(
                parametersCollection,
                context,
                type);

            return new ComponentHtmlContent(result);
        }

        private async Task<IHtmlContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
        {
            var currentInvocation = _serverComponentSerializer.SerializeInvocation(
                invocationId,
                type,
                parametersCollection,
                prerendered: true);

            var result = await _staticComponentRenderer.PrerenderComponentAsync(
                parametersCollection,
                context,
                type);

            return new ComponentHtmlContent(
                _serverComponentSerializer.GetPreamble(currentInvocation),
                result,
                _serverComponentSerializer.GetEpilogue(currentInvocation));
        }

        private IHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
        {
            var serviceProvider = context.RequestServices;
            var currentInvocation = _serverComponentSerializer.SerializeInvocation(invocationId, type, parametersCollection, prerendered: false);

            return new ComponentHtmlContent(_serverComponentSerializer.GetPreamble(currentInvocation));
        }
    }
}
