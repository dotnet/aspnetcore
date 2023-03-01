// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class ComponentRenderer
{
    private static readonly object ComponentSequenceKey = new object();
    private static readonly object InvokedRenderModesKey = new object();

    private readonly StaticComponentRenderer _staticComponentRenderer;
    private readonly ServerComponentSerializer _serverComponentSerializer;
    private readonly IViewBufferScope _viewBufferScope;

    public ComponentRenderer(
        StaticComponentRenderer staticComponentRenderer,
        ServerComponentSerializer serverComponentSerializer,
        IViewBufferScope viewBufferScope)
    {
        _staticComponentRenderer = staticComponentRenderer;
        _serverComponentSerializer = serverComponentSerializer;
        _viewBufferScope = viewBufferScope;
    }

    public async ValueTask<IHtmlContent> RenderComponentAsync(
        ViewContext viewContext,
        Type componentType,
        RenderMode renderMode,
        object parameters)
    {
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(componentType);

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
            RenderMode.WebAssembly => NonPrerenderedWebAssemblyComponent(componentType, parameterView),
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

    private async ValueTask<IHtmlContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var htmlComponent = await _staticComponentRenderer.PrerenderComponentAsync(
            parametersCollection,
            context,
            type);
        return new PrerenderedComponentHtmlContent(_staticComponentRenderer.Dispatcher, htmlComponent, null, null);
    }

    private async Task<IHtmlContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
        }

        var marker = _serverComponentSerializer.SerializeInvocation(
            invocationId,
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await _staticComponentRenderer.PrerenderComponentAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(_staticComponentRenderer.Dispatcher, htmlComponent, marker, null);
    }

    private async ValueTask<IHtmlContent> PrerenderedWebAssemblyComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await _staticComponentRenderer.PrerenderComponentAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(_staticComponentRenderer.Dispatcher, htmlComponent, null, marker);
    }

    private IHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
        }

        var marker = _serverComponentSerializer.SerializeInvocation(invocationId, type, parametersCollection, prerendered: false);
        return new PrerenderedComponentHtmlContent(null, null, marker, null);
    }

    private static IHtmlContent NonPrerenderedWebAssemblyComponent(Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(type, parametersCollection, prerendered: false);
        return new PrerenderedComponentHtmlContent(null, null, null, marker);
    }

    private class PrerenderedComponentHtmlContent : IHtmlContent, IAsyncHtmlContent
    {
        private readonly Dispatcher _dispatcher;
        private readonly HtmlComponent _htmlToEmitOrNull;
        private readonly ServerComponentMarker? _serverMarker;
        private readonly WebAssemblyComponentMarker? _webAssemblyMarker;

        public PrerenderedComponentHtmlContent(
            Dispatcher dispatcher,
            HtmlComponent htmlToEmitOrNull, // If null, we're only emitting the markers
            ServerComponentMarker? serverMarker,
            WebAssemblyComponentMarker? webAssemblyMarker)
        {
            _dispatcher = dispatcher;
            _htmlToEmitOrNull = htmlToEmitOrNull;
            _serverMarker = serverMarker;
            _webAssemblyMarker = webAssemblyMarker;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_serverMarker.HasValue)
            {
                ServerComponentSerializer.AppendPreamble(writer, _serverMarker.Value);
            }
            else if (_webAssemblyMarker.HasValue)
            {
                WebAssemblyComponentSerializer.AppendPreamble(writer, _webAssemblyMarker.Value);
            }

            if (_htmlToEmitOrNull is { } htmlToEmit)
            {
                htmlToEmit.WriteHtmlTo(writer);

                if (_serverMarker.HasValue)
                {
                    ServerComponentSerializer.AppendEpilogue(writer, _serverMarker.Value);
                }
                else if (_webAssemblyMarker.HasValue)
                {
                    WebAssemblyComponentSerializer.AppendPreamble(writer, _webAssemblyMarker.Value);
                }
            }
        }

        public async ValueTask WriteToAsync(TextWriter writer)
        {
            if (_dispatcher is null)
            {
                WriteTo(writer, HtmlEncoder.Default);
            }
            else
            {
                await _dispatcher.InvokeAsync(() => WriteTo(writer, HtmlEncoder.Default));
            }
        }
    }
}
