// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal sealed class ComponentRenderer : IComponentRenderer
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

    private ValueTask<IHtmlContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        return _staticComponentRenderer.PrerenderComponentAsync(
            parametersCollection,
            context,
            type);
    }

    private async Task<IHtmlContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
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

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(ComponentRenderer), ViewBuffer.ViewPageSize);
        ServerComponentSerializer.AppendPreamble(viewBuffer, currentInvocation);
        viewBuffer.AppendHtml(result);
        ServerComponentSerializer.AppendEpilogue(viewBuffer, currentInvocation);

        return viewBuffer;
    }

    private async ValueTask<IHtmlContent> PrerenderedWebAssemblyComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var currentInvocation = WebAssemblyComponentSerializer.SerializeInvocation(
            type,
            parametersCollection,
            prerendered: true);

        var result = await _staticComponentRenderer.PrerenderComponentAsync(
            parametersCollection,
            context,
            type);

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(ComponentRenderer), ViewBuffer.ViewPageSize);
        WebAssemblyComponentSerializer.AppendPreamble(viewBuffer, currentInvocation);
        viewBuffer.AppendHtml(result);
        WebAssemblyComponentSerializer.AppendEpilogue(viewBuffer, currentInvocation);

        return viewBuffer;
    }

    private IHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
        }

        var currentInvocation = _serverComponentSerializer.SerializeInvocation(invocationId, type, parametersCollection, prerendered: false);

        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(ComponentRenderer), ServerComponentSerializer.PreambleBufferSize);
        ServerComponentSerializer.AppendPreamble(viewBuffer, currentInvocation);
        return viewBuffer;
    }

    private IHtmlContent NonPrerenderedWebAssemblyComponent(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var currentInvocation = WebAssemblyComponentSerializer.SerializeInvocation(type, parametersCollection, prerendered: false);
        var viewBuffer = new ViewBuffer(_viewBufferScope, nameof(ComponentRenderer), ServerComponentSerializer.PreambleBufferSize);
        WebAssemblyComponentSerializer.AppendPreamble(viewBuffer, currentInvocation);
        return viewBuffer;
    }
}
