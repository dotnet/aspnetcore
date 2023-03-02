// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

// Wraps the public HtmlRenderer APIs so that the output also gets annotated with prerendering markers.
// This allows the prerendered content to switch later into interactive mode.
// This class also deals with initializing the standard component DI services once per request.
internal sealed class ComponentPrerenderer
{
    private static readonly object ComponentSequenceKey = new object();
    private static readonly object InvokedRenderModesKey = new object();

    private readonly HtmlRenderer _htmlRenderer;
    private readonly ServerComponentSerializer _serverComponentSerializer;
    private readonly object _servicesInitializedLock = new();
    private Task _servicesInitializedTask;

    public ComponentPrerenderer(
        HtmlRenderer htmlRenderer,
        ServerComponentSerializer serverComponentSerializer)
    {
        _serverComponentSerializer = serverComponentSerializer;
        _htmlRenderer = htmlRenderer;
    }

    public async ValueTask<IHtmlContent> PrerenderComponentAsync(
        ViewContext viewContext,
        Type componentType,
        RenderMode prerenderMode,
        ParameterView parameters)
    {
        ArgumentNullException.ThrowIfNull(viewContext);
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(Resources.FormatTypeMustDeriveFromType(componentType, typeof(IComponent)));
        }

        // Make sure we only initialize the services once, but on every call we wait for that process to complete
        var httpContext = viewContext.HttpContext;
        lock (_servicesInitializedLock)
        {
            _servicesInitializedTask ??= InitializeStandardComponentServicesAsync(httpContext);
        }
        await _servicesInitializedTask;

        UpdateSaveStateRenderMode(viewContext, prerenderMode);

        return prerenderMode switch
        {
            RenderMode.Server => NonPrerenderedServerComponent(httpContext, GetOrCreateInvocationId(viewContext), componentType, parameters),
            RenderMode.ServerPrerendered => await PrerenderedServerComponentAsync(httpContext, GetOrCreateInvocationId(viewContext), componentType, parameters),
            RenderMode.Static => await StaticComponentAsync(httpContext, componentType, parameters),
            RenderMode.WebAssembly => NonPrerenderedWebAssemblyComponent(componentType, parameters),
            RenderMode.WebAssemblyPrerendered => await PrerenderedWebAssemblyComponentAsync(httpContext, componentType, parameters),
            _ => throw new ArgumentException(Resources.FormatUnsupportedRenderMode(prerenderMode), nameof(prerenderMode)),
        };
    }

    public async ValueTask<HtmlComponent> PrerenderComponentCoreAsync(
        ParameterView parameters,
        HttpContext httpContext,
        Type componentType)
    {
        try
        {
            return await _htmlRenderer.Dispatcher.InvokeAsync(() =>
                _htmlRenderer.RenderComponentAsync(componentType, parameters));
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
        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);
        return new PrerenderedComponentHtmlContent(_htmlRenderer.Dispatcher, htmlComponent, null, null);
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

        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(_htmlRenderer.Dispatcher, htmlComponent, marker, null);
    }

    private async ValueTask<IHtmlContent> PrerenderedWebAssemblyComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
    {
        var marker = WebAssemblyComponentSerializer.SerializeInvocation(
            type,
            parametersCollection,
            prerendered: true);

        var htmlComponent = await PrerenderComponentCoreAsync(
            parametersCollection,
            context,
            type);

        return new PrerenderedComponentHtmlContent(_htmlRenderer.Dispatcher, htmlComponent, null, marker);
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

    // An implementation of IHtmlContent that holds a reference to a component until we're ready to emit it as HTML to the response.
    // We don't construct the actual HTML until we receive the call to WriteTo.
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

        // For back-compat, we have to supply an implemention of IHtmlContent. However this will only work
        // if you call it on the HtmlRenderer's sync context. The framework itself will not call this directly
        // and will instead use WriteToAsync which deals with dispatching to the sync context.
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

    private static async Task InitializeStandardComponentServicesAsync(HttpContext httpContext)
    {
        var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
        navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

        var authenticationStateProvider = httpContext.RequestServices.GetService<AuthenticationStateProvider>() as IHostEnvironmentAuthenticationStateProvider;
        if (authenticationStateProvider != null)
        {
            var authenticationState = new AuthenticationState(httpContext.User);
            authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }

        // It's important that this is initialized since a component might try to restore state during prerendering
        // (which will obviously not work, but should not fail)
        var componentApplicationLifetime = httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        await componentApplicationLifetime.RestoreStateAsync(new PrerenderComponentApplicationStore());
    }

    private static string GetFullUri(HttpRequest request)
    {
        return UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            request.Path,
            request.QueryString);
    }

    private static string GetContextBaseUri(HttpRequest request)
    {
        var result = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase);

        // PathBase may be "/" or "/some/thing", but to be a well-formed base URI
        // it has to end with a trailing slash
        return result.EndsWith('/') ? result : result += "/";
    }
}
