// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private readonly Dictionary<(int ComponentId, int FrameIndex), string> _namedSubmitEventsByLocation = new();
    private readonly Dictionary<string, (int ComponentId, int FrameIndex)> _namedSubmitEventsByScopeQualifiedName = new(StringComparer.Ordinal);

    internal Task DispatchSubmitEventAsync(string? handlerName, out bool isBadRequest)
    {
        if (string.IsNullOrEmpty(handlerName))
        {
            // This is likely during development if the developer adds <form method=post> without @onsubmit:name,
            // or in production if someone just does a POST request even though there's no UI to trigger it
            isBadRequest = true;
            return ReturnBadRequestAsync("Cannot dispatch the POST request to the Razor Component endpoint, because the POST data does not specify which form is being submitted. To fix this, ensure form elements have an @onsubmit:name attribute with any unique value, or pass a FormHandlerName parameter if using EditForm.");
        }

        if (!_namedSubmitEventsByScopeQualifiedName.TryGetValue(handlerName, out var frameLocation))
        {
            // This may happen if you deploy an app update and someone still on the old page submits a form,
            // or if you're dynamically building the UI and the submitted form doesn't exist the next time
            // the page is rendered
            isBadRequest = true;
            return ReturnBadRequestAsync($"Cannot submit the form '{handlerName}' because no submit handler was found with that name. Ensure forms have a unique @onsubmit:name attribute, or pass the FormHandlerName parameter if using EditForm.");
        }

        isBadRequest = false;
        var eventHandlerId = FindEventHandlerIdForNamedEvent("onsubmit", frameLocation.ComponentId, frameLocation.FrameIndex);
        return eventHandlerId.HasValue
            ? DispatchEventAsync(eventHandlerId.Value, null, EventArgs.Empty, quiesce: true)
            : Task.CompletedTask;
    }

    private Task ReturnBadRequestAsync(string detailedMessage)
    {
        _httpContext.Response.StatusCode = 400;
        _httpContext.Response.ContentType = "text/plain";
        return _httpContext.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
            ? _httpContext.Response.WriteAsync(detailedMessage)
            : Task.CompletedTask;
    }

    private void UpdateNamedEvents(in RenderBatch renderBatch)
    {
        if (renderBatch.NamedEventChanges is { } changes)
        {
            var changesCount = changes.Count;
            var changesArray = changes.Array;
            for (var i = 0; i < changesCount; i++)
            {
                ref var change = ref changesArray[i];
                if (!string.Equals(change.EventType, "onsubmit", StringComparison.Ordinal))
                {
                    continue;
                }

                switch (change.Type)
                {
                    case NamedEventChange.ChangeType.Added:
                    {
                        if (TryCreateScopeQualifiedEventName(change.ComponentId, change.AssignedName, out var scopeQualifiedName))
                        {
                            var location = (change.ComponentId, change.FrameIndex);

                            if (_namedSubmitEventsByScopeQualifiedName.TryAdd(scopeQualifiedName, location))
                            {
                                _namedSubmitEventsByLocation.Add(location, scopeQualifiedName);
                            }
                            else
                            {
                                // We could allow multiple events with the same name, since they are all tracked separately. However
                                // this is most likely a mistake on the developer's part so we will consider it an error.
                                var existingEntry = _namedSubmitEventsByScopeQualifiedName[scopeQualifiedName];
                                throw new InvalidOperationException($"There is more than one named event with the name '{scopeQualifiedName}'. Ensure named events have unique names, or are in scopes with distinct names. The following components both use this name:"
                                    + $"\n - {GenerateComponentPath(existingEntry.ComponentId)}"
                                    + $"\n - {GenerateComponentPath(change.ComponentId)}");
                            }
                        }
                        break;
                    }
                    case NamedEventChange.ChangeType.Removed:
                    {
                        var location = (change.ComponentId, change.FrameIndex);
                        if (_namedSubmitEventsByLocation.Remove(location, out var scopeQualifiedName))
                        {
                            _namedSubmitEventsByScopeQualifiedName.Remove(scopeQualifiedName);
                        }
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Received unknown named event change type {change.Type}");
                }
            }
        }
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
