// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class EndpointHtmlRenderer
{
    private static readonly object ComponentSequenceKey = new object();

    public ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        Type componentType,
        RenderMode prerenderMode,
        ParameterView parameters)
        => PrerenderComponentAsync(httpContext, componentType, prerenderMode, parameters, waitForQuiescence: true);

    public async ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        Type componentType,
        RenderMode prerenderMode,
        ParameterView parameters,
        bool waitForQuiescence)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(Resources.FormatTypeMustDeriveFromType(componentType, typeof(IComponent)));
        }

        // Make sure we only initialize the services once, but on every call we wait for that process to complete
        // This does not have to be threadsafe since it's not valid to call this simultaneously from multiple threads.
        _servicesInitializedTask ??= InitializeStandardComponentServicesAsync(httpContext);
        await _servicesInitializedTask;

        UpdateSaveStateRenderMode(httpContext, prerenderMode);

        try
        {
            var result = prerenderMode switch
            {
                RenderMode.Server => NonPrerenderedServerComponent(httpContext, GetOrCreateInvocationId(httpContext), componentType, parameters),
                RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(httpContext, GetOrCreateInvocationId(httpContext), componentType, parameters),
                RenderMode.Static => await StaticComponentAsync(componentType, parameters),
                RenderMode.WebAssembly => NonPrerenderedWebAssemblyComponent(componentType, parameters),
                RenderMode.WebAssemblyPrerendered => await PrerenderedWebAssemblyComponentAsync(componentType, parameters),
                _ => throw new ArgumentException(Resources.FormatUnsupportedRenderMode(prerenderMode), nameof(prerenderMode)),
            };

            if (waitForQuiescence)
            {
                // Full quiescence, i.e., all tasks completed regardless of streaming SSR
                await result.QuiescenceTask;
            }
            else
            {
                // Just wait for quiescence of the non-streaming subtrees
                await Task.WhenAll(_nonStreamingPendingTasks);
            }

            return result;
        }
        catch (NavigationException navigationException)
        {
            if (httpContext.Response.HasStarted)
            {
                // If we're not doing streaming SSR, this has no choice but to be a fatal error because there's no way to
                // communicate the redirection to the browser.
                // If we are doing streaming SSR, this should not generally happen because if you navigate during the initial
                // synchronous render, the response would not yet have started, and if you do so during some later async
                // phase, we would already have exited this method since streaming SSR means not awaiting quiescence.
                throw new InvalidOperationException(
                    "A navigation command was attempted during prerendering after the server already started sending the response. " +
                    "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                    "response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.");
            }
            else
            {
                httpContext.Response.Redirect(navigationException.Location);
                return PrerenderedComponentHtmlContent.Empty;
            }
        }
    }

    private static ServerComponentInvocationSequence GetOrCreateInvocationId(HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(ComponentSequenceKey, out var result))
        {
            result = new ServerComponentInvocationSequence();
            httpContext.Items[ComponentSequenceKey] = result;
        }

        return (ServerComponentInvocationSequence)result!;
    }

    private async Task<PrerenderedComponentHtmlContent> StaticComponentAsync(Type type, ParameterView parametersCollection)
    {
        var htmlComponent = await Dispatcher.InvokeAsync(() => BeginRenderingComponent(type, parametersCollection));
        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, null, null);
    }

    private async Task<PrerenderedComponentHtmlContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
        }

        // Lazy because we don't actually want to require a whole chain of services including Data Protection
        // to be required unless you actually use Server render mode.
        var serverComponentSerializer = _services.GetRequiredService<ServerComponentSerializer>();

        var marker = serverComponentSerializer.SerializeInvocation(
            invocationId,
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await Dispatcher.InvokeAsync(() => BeginRenderingComponent(type, parametersCollection));
        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, marker, null);
    }

    private async ValueTask<PrerenderedComponentHtmlContent> PrerenderedWebAssemblyComponentAsync(Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await Dispatcher.InvokeAsync(() => BeginRenderingComponent(type, parametersCollection));
        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, null, marker);
    }

    private PrerenderedComponentHtmlContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
    {
        if (!context.Response.HasStarted)
        {
            context.Response.Headers.CacheControl = "no-cache, no-store, max-age=0";
        }

        // Lazy because we don't actually want to require a whole chain of services including Data Protection
        // to be required unless you actually use Server render mode.
        var serverComponentSerializer = _services.GetRequiredService<ServerComponentSerializer>();

        var marker = serverComponentSerializer.SerializeInvocation(invocationId, type, parametersCollection, prerendered: false);
        return new PrerenderedComponentHtmlContent(null, null, marker, null);
    }

    private static PrerenderedComponentHtmlContent NonPrerenderedWebAssemblyComponent(Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(type, parametersCollection, prerendered: false);
        return new PrerenderedComponentHtmlContent(null, null, null, marker);
    }

    // An implementation of IHtmlContent that holds a reference to a component until we're ready to emit it as HTML to the response.
    // We don't construct the actual HTML until we receive the call to WriteTo.
    public class PrerenderedComponentHtmlContent : IHtmlAsyncContent
    {
        private readonly Dispatcher? _dispatcher;
        private readonly HtmlComponent? _htmlToEmitOrNull;
        private readonly ServerComponentMarker? _serverMarker;
        private readonly WebAssemblyComponentMarker? _webAssemblyMarker;

        public static PrerenderedComponentHtmlContent Empty { get; }
            = new PrerenderedComponentHtmlContent(null, HtmlComponent.Empty, null, null);

        public PrerenderedComponentHtmlContent(
            Dispatcher? dispatcher, // If null, we're only emitting the markers
            HtmlComponent? htmlToEmitOrNull, // If null, we're only emitting the markers
            ServerComponentMarker? serverMarker,
            WebAssemblyComponentMarker? webAssemblyMarker)
        {
            _dispatcher = dispatcher;
            _htmlToEmitOrNull = htmlToEmitOrNull;
            _serverMarker = serverMarker;
            _webAssemblyMarker = webAssemblyMarker;
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
                    WebAssemblyComponentSerializer.AppendEpilogue(writer, _webAssemblyMarker.Value);
                }
            }
        }

        public Task QuiescenceTask =>
            _htmlToEmitOrNull is null ? Task.CompletedTask : _htmlToEmitOrNull.QuiescenceTask;
    }
}
