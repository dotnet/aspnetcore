// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
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
using Microsoft.Extensions.Primitives;

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
    private Task? _servicesInitializedTask;

    private HttpContext _httpContext = default!; // Always set at the start of an inbound call
    private string? _formHandler;
    private NamedEvent _capturedNamedEvent;

    // The underlying Renderer always tracks the pending tasks representing *full* quiescence, i.e.,
    // when everything (regardless of streaming SSR) is fully complete. In this subclass we also track
    // the subset of those that are from the non-streaming subtrees, since we want the response to
    // wait for the non-streaming tasks (these ones), then start streaming until full quiescence.
    private readonly List<Task> _nonStreamingPendingTasks = new();

    public EndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        : base(serviceProvider, loggerFactory)
    {
        _services = serviceProvider;
    }

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
        Type? componentType = null,
        string? handler = null,
        IFormCollection? form = null)
    {
        var navigationManager = (IHostEnvironmentNavigationManager)httpContext.RequestServices.GetRequiredService<NavigationManager>();
        navigationManager?.Initialize(GetContextBaseUri(httpContext.Request), GetFullUri(httpContext.Request));

        var authenticationStateProvider = httpContext.RequestServices.GetService<AuthenticationStateProvider>() as IHostEnvironmentAuthenticationStateProvider;
        if (authenticationStateProvider != null)
        {
            var authenticationState = new AuthenticationState(httpContext.User);
            authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }

        var formData = httpContext.RequestServices.GetRequiredService<FormDataProvider>() as IHostEnvironmentFormDataProvider;
        if (handler != null && form != null && formData != null)
        {
            formData.SetFormData(handler, new FormCollectionReadOnlyDictionary(form));
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
        }
    }

    internal void SetFormHandlerName(string name)
    {
        _formHandler = name;
    }

    protected override bool ShouldTrackNamedEventHandlers() => _formHandler != null;

    protected override void TrackNamedEventId(ulong eventHandlerId, int componentId, string eventNameId)
    {
        if (_formHandler == null || !string.Equals(eventNameId, _formHandler, StringComparison.Ordinal))
        {
            // We only track event names when we are deciding how to dispatch an event and when the event
            // matches the identifier for the event we are trying to dispatch.
            return;
        }

        if (_capturedNamedEvent.EventNameId == null)
        {
            // This is the first time we see the event being tracked, we capture it.
            _capturedNamedEvent = new(eventHandlerId, componentId, eventNameId);
            return;
        }

        if (_capturedNamedEvent.ComponentId != componentId)
        {
            // At this point we have already seen this event once. Once a component instance defines a named
            // event, that component is the owner of that event name until we dispatch the event.
            // Dispatching the event happens after we've achieved quiesce.
            // * No two separate components can define an event with the same name identifier.
            // * No other component can define the same named event even if the existing registration
            //   is no longer part of the set of rendered components.
            //   * This gives customers a clear an easy rule about how forms need to be rendered when you
            //     want to support handling POST requests.
            //   * Note that this only affects receiving POST request, it does not impact interactive components
            //     and it does not impact prerendering components.
            try
            {
                // Two components are trying to simultaneously define the same name.
                var state = GetComponentState(_capturedNamedEvent.ComponentId);
                throw new InvalidOperationException(
                    $@"Two different components are trying to define the same named event '{eventNameId}':
'{GenerateComponentPath(state)}'
'{GenerateComponentPath(GetComponentState(componentId))}'");
            }
            catch (ArgumentException)
            {
                // The component that originally defined the name was disposed.
                throw new InvalidOperationException(
                    $"The named event '{eventNameId}' was already defined earlier by a component with id '{_capturedNamedEvent.ComponentId}' but this " +
                    $"component was removed before receiving the dispatched event.");
            }
        }
    }

    internal bool HasCapturedEvent() => _capturedNamedEvent != default;

    internal Task DispatchCapturedEvent()
    {
        if (_capturedNamedEvent == default)
        {
            throw new InvalidOperationException($"No named event handler was captured for '{_formHandler}'.");
        }

        return DispatchEventAsync(_capturedNamedEvent.EventHandlerId, null, EventArgs.Empty, quiesce: true);
    }

    private static string GenerateComponentPath(ComponentState state)
    {
        // We are generating a path from the root component with component type names like:
        // App > Router > RouteView > LayoutView > Index > PartA
        // App > Router > RouteView > LayoutView > MainLayout > NavigationMenu
        // To help developers identify when they have multiple forms with the same handler.
        Stack<string> stack = new();

        for (var current = state; current != null; current = current.ParentComponentState)
        {
            stack.Push(GetName(current));
        }

        var builder = new StringBuilder();
        builder.AppendJoin(" > ", stack);
        return builder.ToString();

        static string GetName(ComponentState current) => current.Component.GetType().Name;
    }

    protected override ComponentState CreateComponentState(int componentId, IComponent component, ComponentState? parentComponentState)
        => new EndpointComponentState(this, componentId, component, parentComponentState);

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
    internal List<Task> NonStreamingPendingTasks => _nonStreamingPendingTasks;

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        if (_streamingUpdatesWriter is { } writer)
        {
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

    private record struct NamedEvent(ulong EventHandlerId, int ComponentId, string EventNameId);

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
