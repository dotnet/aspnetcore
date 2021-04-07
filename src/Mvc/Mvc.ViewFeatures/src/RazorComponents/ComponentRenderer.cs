// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class ComponentRenderer : IComponentRenderer
    {
        private static readonly object ComponentSequenceKey = new object();
        private static readonly object InvokedRenderModesKey = new object();

        private readonly StaticComponentRenderer _staticComponentRenderer;
        private readonly ServerComponentSerializer _serverComponentSerializer;
        private readonly WebAssemblyComponentSerializer _WebAssemblyComponentSerializer;

        public ComponentRenderer(
            StaticComponentRenderer staticComponentRenderer,
            ServerComponentSerializer serverComponentSerializer,
            WebAssemblyComponentSerializer WebAssemblyComponentSerializer)
        {
            _staticComponentRenderer = staticComponentRenderer;
            _serverComponentSerializer = serverComponentSerializer;
            _WebAssemblyComponentSerializer = WebAssemblyComponentSerializer;
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

            UpdateSaveStateRenderMode(viewContext, renderMode);

            return renderMode switch
            {
                RenderMode.Server => NonPrerenderedServerComponent(context, GetOrCreateInvocationId(viewContext), componentType, parameterView),
                RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(context, GetOrCreateInvocationId(viewContext), componentType, parameterView),
                RenderMode.Static => await StaticComponentAsync(context, componentType, parameterView),
                RenderMode.WebAssembly => NonPrerenderedWebAssemblyComponent(context, componentType, parameterView),
                RenderMode.WebAssemblyPrerendered => await PrerenderedWebAssemblyComponentAsync(context, componentType, parameterView),
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

        // Internal for test only
        internal static void UpdateSaveStateRenderMode(ViewContext viewContext, RenderMode mode)
        {
            if (mode == RenderMode.ServerPrerendered || mode == RenderMode.WebAssemblyPrerendered)
            {
                if (!viewContext.Items.TryGetValue(InvokedRenderModesKey, out var result))
                {
                    result = new InvokedRenderModes(mode is RenderMode.ServerPrerendered ?
                        InvokedRenderModes.Mode.Server :
                        InvokedRenderModes.Mode.WebAssembly);

                    viewContext.Items[InvokedRenderModesKey] = result;
                }
                else
                {
                    var currentInvocation = mode is RenderMode.ServerPrerendered ?
                        InvokedRenderModes.Mode.Server :
                        InvokedRenderModes.Mode.WebAssembly;

                    var invokedMode = (InvokedRenderModes)result;
                    if (invokedMode.Value != currentInvocation)
                    {
                        invokedMode.Value = InvokedRenderModes.Mode.ServerAndWebAssembly;
                    }
                }
            }
        }

        internal static InvokedRenderModes.Mode GetPersistStateRenderMode(ViewContext viewContext)
        {
            if (viewContext.Items.TryGetValue(InvokedRenderModesKey, out var result))
            {
                return ((InvokedRenderModes)result).Value;
            }
            else
            {
                return InvokedRenderModes.Mode.None;
            }
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
            if (!context.Response.HasStarted)
            {
                context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, max-age=0";
            }

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

        private async Task<IHtmlContent> PrerenderedWebAssemblyComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
        {
            var currentInvocation = _WebAssemblyComponentSerializer.SerializeInvocation(
                type,
                parametersCollection,
                prerendered: true);

            var result = await _staticComponentRenderer.PrerenderComponentAsync(
                parametersCollection,
                context,
                type);

            return new ComponentHtmlContent(
                _WebAssemblyComponentSerializer.GetPreamble(currentInvocation),
                result,
                _WebAssemblyComponentSerializer.GetEpilogue(currentInvocation));
        }

        private IHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, max-age=0";
            }

            var currentInvocation = _serverComponentSerializer.SerializeInvocation(invocationId, type, parametersCollection, prerendered: false);

            return new ComponentHtmlContent(_serverComponentSerializer.GetPreamble(currentInvocation));
        }

        private IHtmlContent NonPrerenderedWebAssemblyComponent(HttpContext context, Type type, ParameterView parametersCollection)
        {
            var currentInvocation = _WebAssemblyComponentSerializer.SerializeInvocation(type, parametersCollection, prerendered: false);

            return new ComponentHtmlContent(_WebAssemblyComponentSerializer.GetPreamble(currentInvocation));
        }
    }

    internal class InvokedRenderModes
    {
        public InvokedRenderModes(Mode mode)
        {
            Value = mode;
        }

        public Mode Value { get; set; }

        internal enum Mode
        {
            None,
            Server,
            WebAssembly,
            ServerAndWebAssembly
        }
    }
}
