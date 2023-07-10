// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
    private readonly Dictionary<ulong, string> _namedEventsByEventHandlerId = new();
    private readonly Dictionary<string, ulong> _namedEventsByEventHandlerName = new(StringComparer.Ordinal);

    private Task? _servicesInitializedTask;

    private HttpContext _httpContext = default!; // Always set at the start of an inbound call

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

        if (httpContext.RequestServices.GetService<AuthenticationStateProvider>() is IHostEnvironmentAuthenticationStateProvider authenticationStateProvider)
        {
            var authenticationState = new AuthenticationState(httpContext.User);
            authenticationStateProvider.SetAuthenticationState(Task.FromResult(authenticationState));
        }

        if (handler != null && form != null)
        {
            httpContext.RequestServices.GetRequiredService<HttpContextFormDataProvider>()
                .SetFormData(handler, new FormCollectionReadOnlyDictionary(form));
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
        }
    }

    internal Task DispatchSubmitEventAsync(string? handlerName)
    {
        if (string.IsNullOrEmpty(handlerName))
        {
            // Currently this also happens if you forget to add the hidden field, but soon we'll do that automatically, so the
            // message is designed around that.
            throw new InvalidOperationException($"Cannot dispatch the POST request to the Razor Component endpoint, because the POST data does not specify which form is being submitted. To fix this, ensure form elements have an @onsubmit:name attribute with any unique value, or pass a Name parameter if using EditForm.");
        }

        if (!_namedEventsByEventHandlerName.TryGetValue(handlerName, out var eventHandlerId))
        {
            // This might only be possible if you deploy an app update and someone tries to submit
            // an old version of a form, and your new app no longer has a matching name
            throw new InvalidOperationException($"Cannot submit the form '{handlerName}' because no submit handler was found. Ensure forms have a unique @onsubmit:name attribute, or pass the Name parameter if using EditForm.");
        }

        return DispatchEventAsync(eventHandlerId, null, EventArgs.Empty, quiesce: true);
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
        UpdateNamedEventCapture(in renderBatch);

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

    private void UpdateNamedEventCapture(in RenderBatch renderBatch)
    {
        // Remove disposed ones
        var disposedEventHandlerCount = renderBatch.DisposedEventHandlerIDs.Count;
        var disposedEventHandlerArray = renderBatch.DisposedEventHandlerIDs.Array;
        for (var i = 0; i < disposedEventHandlerCount; i++)
        {
            var id = disposedEventHandlerArray[i];
            if (_namedEventsByEventHandlerId.Remove(id, out var name))
            {
                _namedEventsByEventHandlerName.Remove(name);
            }
        }

        // Find and add new ones
        // TODO: Instead of scanning the current rendertree for each updated component,
        // we could observe the frames during diffing and track them as some separate
        // feature of the batch
        var updatedComponentCount = renderBatch.UpdatedComponents.Count;
        var updatedComponentArray = renderBatch.UpdatedComponents.Array;
        for (var i = 0; i < updatedComponentCount; i++)
        {
            // TODO: Bear in mind that a given component may occur multiple times in this list
            // based on how many times it was updated. Would be better if the names were tracked
            // within individual edits/diffs so you only considered the changes once.
            ref var updatedComponent = ref updatedComponentArray[i];
            var currentFrames = GetCurrentRenderTreeFrames(updatedComponent.ComponentId, optional: true);
            if (!currentFrames.HasValue)
            {
                continue;
            }

            var currentFramesCount = currentFrames.Value.Count;
            var currentFramesArray = currentFrames.Value.Array;
            for (var frameIndex = 0; frameIndex < currentFramesCount; frameIndex++)
            {
                // TODO: Would MemoryExtensions.IndexOf help?
                ref var frame = ref currentFramesArray[frameIndex];
                if (frame.FrameType == RenderTreeFrameType.NameForEventHandler)
                {
                    // Find the enclosing matching event
                    ulong? foundEventHandlerId = default;
                    for (var scanIndex = frameIndex - 1; scanIndex >= 0; scanIndex--)
                    {
                        ref var scanFrame = ref currentFramesArray[scanIndex];
                        if (scanFrame.FrameType == RenderTreeFrameType.Element)
                        {
                            break;
                        }
                        else if (scanFrame.FrameType == RenderTreeFrameType.Attribute
                            && scanFrame.AttributeEventHandlerId != default
                            && string.Equals(scanFrame.AttributeName, frame.NamedEventHandlerEventType, StringComparison.Ordinal))
                        {
                            foundEventHandlerId = scanFrame.AttributeEventHandlerId;
                            break;
                        }
                    }

                    if (foundEventHandlerId.HasValue)
                    {
                        // This is not right as it will fail if the component renders multiple times and tries
                        // to change the event handler ID. Need to track at the point we assign event handler IDs.
                        if (_namedEventsByEventHandlerId.TryAdd(foundEventHandlerId.Value, frame.NamedEventHandlerEventName))
                        {
                            _namedEventsByEventHandlerName.Add(frame.NamedEventHandlerEventName, foundEventHandlerId.Value);
                        }
                        else if (_namedEventsByEventHandlerName.TryGetValue(frame.NamedEventHandlerEventName, out var existingId)
                            && existingId != foundEventHandlerId.Value)
                        {
                            throw new InvalidOperationException($"There is more than one named event with the name '{frame.NamedEventHandlerEventName}'. Ensure named events have unique names.");
                        }
                    }
                }
            }
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
