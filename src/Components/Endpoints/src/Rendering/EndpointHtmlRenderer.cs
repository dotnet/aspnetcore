// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// A <see cref="StaticHtmlRenderer"/> subclass which is also the implementation of the
/// <see cref="IComponentPrerenderer"/> DI service. This is the underlying mechanism shared by:
///
/// * Html.RenderComponentAsync (the earliest prerendering mechanism - a Razor HTML helper)
/// * ComponentTagHelper (the primary prerendering mechanism before .NET 8)
/// * RazorComponentResult and RazorComponentEndpoint (the primary prerendering mechanisms since .NET 8)
///
/// EndpointHtmlRenderer wraps the underlying <see cref="Web.HtmlRenderer"/> mechanism, annotating the
/// output with prerendering markers so the content can later switch into interactive mode when used with
/// blazor.*.js. It also deals with initializing the standard component DI services once per request.
/// </summary>
internal partial class EndpointHtmlRenderer : StaticHtmlRenderer, IComponentPrerenderer
{
    private readonly IServiceProvider _services;
    private readonly RazorComponentsServiceOptions _options;
    private Task? _servicesInitializedTask;
    private HttpContext _httpContext = default!; // Always set at the start of an inbound call
    private ResourceAssetCollection? _resourceCollection;

    // The underlying Renderer always tracks the pending tasks representing *full* quiescence, i.e.,
    // when everything (regardless of streaming SSR) is fully complete. In this subclass we also track
    // the subset of those that are from the non-streaming subtrees, since we want the response to
    // wait for the non-streaming tasks (these ones), then start streaming until full quiescence.
    private readonly List<Task> _nonStreamingPendingTasks = new();

    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        _services = serviceProvider;
        _options = serviceProvider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>().Value;
    }

    internal HttpContext? HttpContext => _httpContext;

    private void SetHttpContext(HttpContext httpContext)
    {
        if (_httpContext is null)
        {
            _httpContext = httpContext;
        }
        else if (_httpContext != httpContext)
        {
            throw new InvalidOperationException("The HttpContext cannot change value once assigned.");
        }
    }

    internal static async Task InitializeStandardComponentServicesAsync(
        HttpContext httpContext,
        [DynamicallyAccessedMembers(Component)] Type? componentType = null,
        string? handler = null,
        IFormCollection? form = null)
    {
        var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
        navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

        var authenticationStateProvider = httpContext.RequestServices.GetService<AuthenticationStateProvider>();
        if (authenticationStateProvider is IHostEnvironmentAuthenticationStateProvider hostEnvironmentAuthenticationStateProvider)
        {
            var authenticationState = new AuthenticationState(httpContext.User);
            hostEnvironmentAuthenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }

        if (authenticationStateProvider != null)
        {
            var authStateListeners = httpContext.RequestServices.GetServices<IHostEnvironmentAuthenticationStateProvider>();
            Task<AuthenticationState>? authStateTask = null;
            foreach (var authStateListener in authStateListeners)
            {
                authStateTask ??= authenticationStateProvider.GetAuthenticationStateAsync();
                authStateListener.SetAuthenticationState(authStateTask);
            }
        }

        InitializeResourceCollection(httpContext);

        if (handler != null && form != null)
        {
            httpContext.RequestServices.GetRequiredService<HttpContextFormDataProvider>()
                .SetFormData(handler, new FormCollectionReadOnlyDictionary(form), form.Files);
        }

        if (httpContext.RequestServices.GetService<AntiforgeryStateProvider>() is EndpointAntiforgeryStateProvider antiforgery)
        {
            antiforgery.SetRequestContext(httpContext);
        }

        // It's important that this is initialized since a component might try to restore state during prerendering
        // (which will obviously not work, but should not fail)
        var componentApplicationLifetime = httpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();
        await componentApplicationLifetime.RestoreStateAsync(new PrerenderComponentApplicationStore());

        if (componentType != null)
        {
            // Saving RouteData to avoid routing twice in Router component
            var routingStateProvider = httpContext.RequestServices.GetRequiredService<EndpointRoutingStateProvider>();
            routingStateProvider.RouteData = new RouteData(componentType, httpContext.GetRouteData().Values);
            if (httpContext.GetEndpoint() is RouteEndpoint routeEndpoint)
            {
                routingStateProvider.RouteData.Template = routeEndpoint.RoutePattern.RawText;
            }
        }
    }

    private static void InitializeResourceCollection(HttpContext httpContext)
    {

        var endpoint = httpContext.GetEndpoint();
        var resourceCollection = GetResourceCollection(httpContext);
        var resourceCollectionUrl = resourceCollection != null && endpoint != null ?
            endpoint.Metadata.GetMetadata<ResourceCollectionUrlMetadata>() :
            null;

        var resourceCollectionProvider = resourceCollectionUrl != null ? httpContext.RequestServices.GetService<ResourceCollectionProvider>() : null;
        if (resourceCollectionUrl != null && resourceCollectionProvider != null)
        {
            resourceCollectionProvider.SetResourceCollectionUrl(resourceCollectionUrl.Url);
            resourceCollectionProvider.SetResourceCollection(resourceCollection ?? ResourceAssetCollection.Empty);
        }
    }

    protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
        => new EndpointComponentState(this, componentId, component, parentComponentState);

    /// <inheritdoc/>
    protected override ResourceAssetCollection Assets =>
        _resourceCollection ??= GetResourceCollection(_httpContext) ?? base.Assets;

    private static ResourceAssetCollection? GetResourceCollection(HttpContext httpContext) => httpContext.GetEndpoint()?.Metadata.GetMetadata<ResourceAssetCollection>();

    protected override void AddPendingTask(ComponentState? componentState, Task task)
    {
        var streamRendering = componentState is null
            ? false
            : ((EndpointComponentState)componentState).StreamRendering;

        if (!streamRendering)
        {
            _nonStreamingPendingTasks.Add(task);
        }

        // We still need to determine full quiescence, so always let the base renderer track this task too
        base.AddPendingTask(componentState, task);
    }

    // For tests only
    internal Task? NonStreamingPendingTasksCompletion;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        UpdateNamedSubmitEvents(in renderBatch);

        if (_streamingUpdatesWriter is { } writer)
        {
            // Important: SendBatchAsStreamingUpdate *must* be invoked synchronously
            // before any 'await' in this method. That's enforced by the compiler
            // (the method has an 'in' parameter) but even if it wasn't, it would still
            // be important, because the RenderBatch buffers may be overwritten as soon
            // as we yield the sync context. The only alternative would be to clone the
            // batch deeply, or serialize it synchronously (e.g., via RenderBatchWriter).
            SendBatchAsStreamingUpdate(renderBatch, writer);
            return FlushThenComplete(writer, base.UpdateDisplayAsync(renderBatch));
        }
        else
        {
            return base.UpdateDisplayAsync(renderBatch);
        }

        // Workaround for methods with "in" parameters not being allowed to be async
        // We resolve the "result" first and then combine it with the FlushAsync task here
        static async Task FlushThenComplete(TextWriter writerToFlush, Task completion)
        {
            await writerToFlush.FlushAsync();
            await completion;
        }
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

    private sealed class FormCollectionReadOnlyDictionary : IReadOnlyDictionary<string, StringValues>
    {
        private readonly IFormCollection _form;
        private List<StringValues>? _values;

        public FormCollectionReadOnlyDictionary(IFormCollection form)
        {
            _form = form;
        }

        public StringValues this[string key] => _form[key];

        public IEnumerable<string> Keys => _form.Keys;

        public IEnumerable<StringValues> Values => _values ??= MaterializeValues(_form);

        private static List<StringValues> MaterializeValues(IFormCollection form)
        {
            var result = new List<StringValues>(form.Keys.Count);
            foreach (var key in form.Keys)
            {
                result.Add(form[key]);
            }

            return result;
        }

        public int Count => _form.Count;

        public bool ContainsKey(string key)
        {
            return _form.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return _form.GetEnumerator();
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
        {
            return _form.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _form.GetEnumerator();
        }
    }
}
