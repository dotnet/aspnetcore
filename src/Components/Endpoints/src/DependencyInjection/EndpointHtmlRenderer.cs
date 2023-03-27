// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// An <see cref="HtmlRendererCore"/> subclass that is used when prerendering on an endpoint
/// or for the component tag helper. It knows how to annotate the output with prerendering
/// markers so the content can later switch into interactive mode. It also deals with initializing
/// the standard component DI services once per request.
/// </summary>
internal sealed class EndpointHtmlRenderer : HtmlRendererCore, IComponentPrerenderer
{
    private static readonly object ComponentSequenceKey = new object();
    private static readonly object InvokedRenderModesKey = new object();

    private readonly IServiceProvider _services;
    private Task? _servicesInitializedTask;

    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        _services = serviceProvider;
    }

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
        _servicesInitializedTask ??= InitializeStandardComponentServicesAsync(httpContext);
        await _servicesInitializedTask;

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

    public async ValueTask<IHtmlContent> PrerenderPersistedStateAsync(HttpContext httpContext, PersistedStateSerializationMode serializationMode)
    {
        // First we resolve "infer" mode to a specific mode
        if (serializationMode == PersistedStateSerializationMode.Infer)
        {
            switch (GetPersistStateRenderMode(httpContext))
            {
                case InvokedRenderModes.Mode.None:
                    return ComponentStateHtmlContent.Empty;
                case InvokedRenderModes.Mode.ServerAndWebAssembly:
                    throw new InvalidOperationException(
                        Resources.FailedToInferComponentPersistenceMode);
                case InvokedRenderModes.Mode.Server:
                    serializationMode = PersistedStateSerializationMode.Server;
                    break;
                case InvokedRenderModes.Mode.WebAssembly:
                    serializationMode = PersistedStateSerializationMode.WebAssembly;
                    break;
                default:
                    throw new InvalidOperationException("Invalid persistence mode");
            }
        }

        // Now given the mode, we obtain a particular store for that mode
        var store = serializationMode switch
        {
            PersistedStateSerializationMode.Server =>
                new ProtectedPrerenderComponentApplicationStore(httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>()),
            PersistedStateSerializationMode.WebAssembly =>
                new PrerenderComponentApplicationStore(),
            _ =>
                throw new InvalidOperationException("Invalid persistence mode.")
        };

        // Finally, persist the state and return the HTML content
        var manager = httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        await manager.PersistStateAsync(store, Dispatcher);
        return new ComponentStateHtmlContent(store);
    }

    private async ValueTask<HtmlComponent> PrerenderComponentCoreAsync(
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

    // Internal for test only
    internal static void UpdateSaveStateRenderMode(HttpContext httpContext, RenderMode mode)
    {
        // TODO: This will all have to change when we support multiple render modes in the same response
        if (mode == RenderMode.ServerPrerendered || mode == RenderMode.WebAssemblyPrerendered)
        {
            if (!httpContext.Items.TryGetValue(InvokedRenderModesKey, out var result))
            {
                result = new InvokedRenderModes(mode is RenderMode.ServerPrerendered ?
                    InvokedRenderModes.Mode.Server :
                    InvokedRenderModes.Mode.WebAssembly);

                httpContext.Items[InvokedRenderModesKey] = result;
            }
            else
            {
                var currentInvocation = mode is RenderMode.ServerPrerendered ?
                    InvokedRenderModes.Mode.Server :
                    InvokedRenderModes.Mode.WebAssembly;

                var invokedMode = (InvokedRenderModes)result!;
                if (invokedMode.Value != currentInvocation)
                {
                    invokedMode.Value = InvokedRenderModes.Mode.ServerAndWebAssembly;
                }
            }
        }
    }

    internal static InvokedRenderModes.Mode GetPersistStateRenderMode(HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(InvokedRenderModesKey, out var result)
            ? ((InvokedRenderModes)result!).Value
            : InvokedRenderModes.Mode.None;
    }

    private async ValueTask<IHtmlAsyncContent> StaticComponentAsync(HttpContext context, Type type, ParameterView parametersCollection)
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

    private sealed class ComponentStateHtmlContent : IHtmlContent
    {
        private PrerenderComponentApplicationStore? _store;

        public static ComponentStateHtmlContent Empty { get; }
            = new ComponentStateHtmlContent(null);

        public ComponentStateHtmlContent(PrerenderComponentApplicationStore? store)
        {
            _store = store;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (_store != null)
            {
                writer.Write("<!--Blazor-Component-State:");
                writer.Write(_store.PersistedState);
                writer.Write("-->");
                _store = null;
            }
        }
    }
}
