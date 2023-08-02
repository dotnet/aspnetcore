// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private readonly Dictionary<(int ComponentId, int FrameIndex), string> _namedSubmitEventsByLocation = new();
    private readonly Dictionary<string, HashSet<(int ComponentId, int FrameIndex)>> _namedSubmitEventsByScopeQualifiedName = new(StringComparer.Ordinal);

    internal Task DispatchSubmitEventAsync(string? handlerName, out bool isBadRequest)
    {
        if (string.IsNullOrEmpty(handlerName))
        {
            // This is likely during development if the developer adds <form method=post> without @formname,
            // or in production if someone just does a POST request even though there's no UI to trigger it
            isBadRequest = true;
            return ReturnErrorResponse("The POST request does not specify which form is being submitted. To fix this, ensure <form> elements have a @formname attribute with any unique value, or pass a FormName parameter if using <EditForm>.");
        }

        if (!_namedSubmitEventsByScopeQualifiedName.TryGetValue(handlerName, out var locationsForName) || locationsForName.Count == 0)
        {
            // This may happen if you deploy an app update and someone still on the old page submits a form,
            // or if you're dynamically building the UI and the submitted form doesn't exist the next time
            // the page is rendered
            isBadRequest = true;
            return ReturnErrorResponse($"Cannot submit the form '{handlerName}' because no form on the page currently has that name.");
        }

        if (locationsForName.Count > 1)
        {
            // We could allow multiple events with the same name, since they are all tracked separately. However
            // this is most likely a mistake on the developer's part so we will consider it an error.
            // This is an internal server error, not a bad request, because the application itself is at fault
            // and needs to find out about it. End users can't trigger this unless the app has a bug.
            throw new InvalidOperationException(CreateMessageForAmbiguousNamedSubmitEvent(handlerName, locationsForName));
        }

        isBadRequest = false;
        var frameLocation = locationsForName.Single();
        var eventHandlerId = FindEventHandlerIdForNamedEvent("onsubmit", frameLocation.ComponentId, frameLocation.FrameIndex);
        return eventHandlerId.HasValue
            ? DispatchEventAsync(eventHandlerId.Value, null, EventArgs.Empty, waitForQuiescence: true)
            : Task.CompletedTask;
    }

    private string CreateMessageForAmbiguousNamedSubmitEvent(string scopeQualifiedName, IEnumerable<(int ComponentId, int FrameIndex)> locations)
    {
        var sb = new StringBuilder($"There is more than one named submit event with the name '{scopeQualifiedName}'. Ensure named submit events have unique names, or are in scopes with distinct names. The following components use this name:");

        foreach (var location in locations)
        {
            sb.Append(CultureInfo.InvariantCulture, $"\n - {GenerateComponentPath(location.ComponentId)}");
        }

        return sb.ToString();
    }

    private Task ReturnErrorResponse(string detailedMessage)
    {
        _httpContext.Response.StatusCode = 400;
        _httpContext.Response.ContentType = "text/plain";
        return _httpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
            ? _httpContext.Response.WriteAsync(detailedMessage)
            : Task.CompletedTask;
    }

    private void UpdateNamedSubmitEvents(in RenderBatch renderBatch)
    {
        if (renderBatch.NamedEventChanges is { } changes)
        {
            ProcessNamedSubmitEventRemovals(changes);
            ProcessNamedSubmitEventAdditions(changes);
        }
    }

    private void ProcessNamedSubmitEventRemovals(ArrayRange<NamedEventChange> changes)
    {
        var changesCount = changes.Count;
        var changesArray = changes.Array;
        for (var i = 0; i < changesCount; i++)
        {
            ref var change = ref changesArray[i];
            if (change.ChangeType == NamedEventChangeType.Removed
                && string.Equals(change.EventType, "onsubmit", StringComparison.Ordinal))
            {
                var location = (change.ComponentId, change.FrameIndex);
                if (_namedSubmitEventsByLocation.Remove(location, out var scopeQualifiedName))
                {
                    var locationsForName = _namedSubmitEventsByScopeQualifiedName[scopeQualifiedName];
                    locationsForName.Remove(location);
                    if (locationsForName.Count == 0)
                    {
                        _namedSubmitEventsByScopeQualifiedName.Remove(scopeQualifiedName);
                    }
                }
            }
        }
    }

    private void ProcessNamedSubmitEventAdditions(ArrayRange<NamedEventChange> changes)
    {
        var changesCount = changes.Count;
        var changesArray = changes.Array;
        for (var i = 0; i < changesCount; i++)
        {
            ref var change = ref changesArray[i];
            if (change.ChangeType == NamedEventChangeType.Added
                && string.Equals(change.EventType, "onsubmit", StringComparison.Ordinal))
            {
                if (TryCreateScopeQualifiedEventName(change.ComponentId, change.AssignedName, out var scopeQualifiedName))
                {
                    var locationsForName = GetOrAddNewToDictionary(_namedSubmitEventsByScopeQualifiedName, scopeQualifiedName);
                    var location = (change.ComponentId, change.FrameIndex);
                    if (!locationsForName.Add(location))
                    {
                        // This shouldn't be possible, since each NamedEvent frame innately has a distinct location
                        throw new InvalidOperationException($"A single named submit event is tracked more than once at the same location.");
                    }

                    _namedSubmitEventsByLocation.Add(location, scopeQualifiedName);
                }
            }
        }
    }

    private static TVal GetOrAddNewToDictionary<TKey, TVal>(Dictionary<TKey, TVal> dictionary, TKey key) where TKey: notnull where TVal: new()
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = new();
            dictionary.Add(key, value);
        }

        return value;
    }

    private ulong? FindEventHandlerIdForNamedEvent(string eventType, int componentId, int frameIndex)
    {
        var frames = GetCurrentRenderTreeFrames(componentId);
        ref var frame = ref frames.Array[frameIndex];

        if (frame.FrameType != RenderTreeFrameType.NamedEvent)
        {
            // This should not be possible, as the system doesn't create a way that the location could be wrong. But if it happens, we want to know.
            throw new InvalidOperationException($"The named value frame for component '{componentId}' at index '{frameIndex}' unexpectedly matches a frame of type '{frame.FrameType}'.");
        }

        if (!string.Equals(frame.NamedEventType, eventType, StringComparison.Ordinal))
        {
            // This should not be possible, as currently we are only tracking name-values with the expected name. But if it happens, we want to know.
            throw new InvalidOperationException($"Expected a named value with name '{eventType}' but found the name '{frame.NamedEventType}'.");
        }

        for (var i = frameIndex - 1; i >= 0; i--)
        {
            ref var candidate = ref frames.Array[i];
            if (candidate.FrameType == RenderTreeFrameType.Attribute)
            {
                if (candidate.AttributeEventHandlerId > 0 && string.Equals(candidate.AttributeName, eventType, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.AttributeEventHandlerId;
                }
            }
            else if (candidate.FrameType == RenderTreeFrameType.Element)
            {
                break;
            }
        }

        // No match found
        return default;
    }

    private string GenerateComponentPath(int componentId)
    {
        // We are generating a path from the root component with component type names like:
        // App > Router > RouteView > LayoutView > Index > PartA
        // App > Router > RouteView > LayoutView > MainLayout > NavigationMenu
        // To help developers identify when they have multiple forms with the same handler.
        Stack<string> stack = new();

        for (var current = GetComponentState(componentId); current != null; current = current.ParentComponentState)
        {
            stack.Push(GetName(current));
        }

        var builder = new StringBuilder();
        builder.AppendJoin(" > ", stack);
        return builder.ToString();

        static string GetName(ComponentState current) => current.Component.GetType().Name;
    }
}
