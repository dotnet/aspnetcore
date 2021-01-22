// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class ComponentRenderer : IComponentRenderer
    {
        private static readonly object ComponentSequenceKey = new object();
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

            var manager = context.RequestServices.GetRequiredService<ComponentApplicationLifetime>();
            var store = new PrerenderComponentApplicationStore();
            await manager.RestoreStateAsync(store);

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

    internal class PrerenderComponentApplicationStore : IComponentApplicationStateStore
    {
        private readonly Dictionary<string, string> _existingState;

        public PrerenderComponentApplicationStore() { _existingState = new(); }

        public PrerenderComponentApplicationStore(string existingState)
        {
            _existingState = JsonSerializer.Deserialize<Dictionary<string, string>>(Convert.FromBase64String(existingState));
        }

        public IHtmlContent PersistedState { get; private set; }

        public IDictionary<string, string> GetPersistedState()
        {
            return _existingState ?? throw new InvalidOperationException("The store was not initialized with any state.");
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, string> state)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(state);

            var result = Convert.ToBase64String(bytes);
            PersistedState = new StateComment(result);
            return Task.CompletedTask;
        }

        private class StateComment : IHtmlContent
        {
            private string _result;

            public StateComment(string result)
            {
                _result = result;
            }

            public void WriteTo(TextWriter writer, HtmlEncoder encoder)
            {
                writer.Write("<!--Blazor-Component-State:");
                writer.Write(_result);
                writer.Write("-->");
            }
        }
    }
}
