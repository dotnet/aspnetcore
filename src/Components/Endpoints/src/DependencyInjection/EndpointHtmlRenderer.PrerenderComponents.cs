// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class EndpointHtmlRenderer
{
    private static readonly object ComponentSequenceKey = new object();

    public async ValueTask<IHtmlAsyncContent> PrerenderComponentAsync(
        HttpContext httpContext,
        Type componentType,
        RenderMode prerenderMode,
        ParameterView parameters)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(Resources.FormatTypeMustDeriveFromType(componentType, typeof(IComponent)));
        }

        // Make sure we only initialize the services once, but on every call we wait for that process to complete
        // This does not have to be threadsafe since it's not valid to call this simultaneously from multiple threads.
        await InitializeStandardComponentServicesAsync(httpContext);

        UpdateSaveStateRenderMode(httpContext, prerenderMode);

        return prerenderMode switch
        {
            RenderMode.Server => NonPrerenderedServerComponent(httpContext, GetOrCreateInvocationId(httpContext), componentType, parameters),
            RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(httpContext, GetOrCreateInvocationId(httpContext), componentType, parameters),
            RenderMode.Static => await StaticComponentAsync(httpContext, componentType, parameters),
            RenderMode.WebAssembly => NonPrerenderedWebAssemblyComponent(componentType, parameters),
            RenderMode.WebAssemblyPrerendered => await PrerenderedWebAssemblyComponentAsync(httpContext, componentType, parameters),
            _ => throw new ArgumentException(Resources.FormatUnsupportedRenderMode(prerenderMode), nameof(prerenderMode)),
        };
    }

    internal async ValueTask<HtmlComponent> PrerenderComponentCoreAsync(
        ParameterView parameters,
        HttpContext httpContext,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
    {
        try
        {
            return await Dispatcher.InvokeAsync(async () =>
            {
                var content = BeginRenderingComponent(componentType, parameters);
                await content.WaitForQuiescenceAsync();
                return content;
            });
        }
        catch (NavigationException navigationException)
        {
            // Navigation was attempted during prerendering.
            if (httpContext.Response.HasStarted)
            {
                // We can't perform a redirect as the server already started sending the response.
                // This is considered an application error as the developer should buffer the response until
                // all components have rendered.
                throw new InvalidOperationException("A navigation command was attempted during prerendering after the server already started sending the response. " +
                    "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                    "response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.", navigationException);
            }

            httpContext.Response.Redirect(navigationException.Location);
            return HtmlComponent.Empty;
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

    internal async ValueTask<IHtmlAsyncContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);
        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, null, null);
    }

    private async Task<IHtmlAsyncContent> PrerenderedServerComponentAsync(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
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

        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, marker, null);
    }

    private async ValueTask<IHtmlAsyncContent> PrerenderedWebAssemblyComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(Dispatcher, htmlComponent, null, marker);
    }

    private IHtmlAsyncContent NonPrerenderedServerComponent(HttpContext context, ServerComponentInvocationSequence invocationId, Type type, ParameterView parametersCollection)
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

    private static IHtmlAsyncContent NonPrerenderedWebAssemblyComponent(Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(type, parametersCollection, prerendered: false);
        return new PrerenderedComponentHtmlContent(null, null, null, marker);
    }

    // An implementation of IHtmlContent that holds a reference to a component until we're ready to emit it as HTML to the response.
    // We don't construct the actual HTML until we receive the call to WriteTo.
    private class PrerenderedComponentHtmlContent : IHtmlAsyncContent
    {
        private readonly Dispatcher? _dispatcher;
        private readonly HtmlComponent? _htmlToEmitOrNull;
        private readonly ServerComponentMarker? _serverMarker;
        private readonly WebAssemblyComponentMarker? _webAssemblyMarker;

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
    }
}
