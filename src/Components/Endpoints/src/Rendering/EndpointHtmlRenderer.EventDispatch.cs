// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using System.Text;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer
{
    private const string OnSubmitNameAttribute = "@onsubmit:name";
    private readonly Dictionary<(int ComponentId, int FrameIndex), string> _namedEventsByLocation = new();
    private readonly Dictionary<string, (int ComponentId, int FrameIndex)> _namedEventsByAssignedName = new(StringComparer.Ordinal);

    internal Task DispatchSubmitEventAsync(string? handlerName)
    {
        if (string.IsNullOrEmpty(handlerName))
        {
            // Currently this also happens if you forget to add the hidden field, but soon we'll do that automatically, so the
            // message is designed around that.
            throw new InvalidOperationException($"Cannot dispatch the POST request to the Razor Component endpoint, because the POST data does not specify which form is being submitted. To fix this, ensure form elements have an @onsubmit:name attribute with any unique value, or pass a Name parameter if using EditForm.");
        }

        if (!_namedEventsByAssignedName.TryGetValue(handlerName, out var frameLocation))
        {
            // This might only be possible if you deploy an app update and someone tries to submit
            // an old version of a form, and your new app no longer has a matching name
            throw new InvalidOperationException($"Cannot submit the form '{handlerName}' because no submit handler was found with that name. Ensure forms have a unique @onsubmit:name attribute, or pass the Name parameter if using EditForm.");
        }

        var eventHandlerId = FindEventHandlerIdForNamedEvent(frameLocation.ComponentId, frameLocation.FrameIndex);
        return DispatchEventAsync(eventHandlerId, null, EventArgs.Empty, quiesce: true);
    }

    private void UpdateNamedEvents(in RenderBatch renderBatch)
    {
        if (renderBatch.RemovedNamedValues is { } removed)
        {
            var removedCount = removed.Count;
            var removedArray = removed.Array;
            for (var i = 0; i < removedCount; i++)
            {
                ref var removedEntry = ref removedArray[i];
                if (string.Equals(removedEntry.Name, OnSubmitNameAttribute, StringComparison.Ordinal))
                {
                    var location = (removedEntry.ComponentId, removedEntry.FrameIndex);
                    if (_namedEventsByLocation.Remove(location, out var assignedName))
                    {
                        _namedEventsByAssignedName.Remove(assignedName);
                    }
                }
            }
        }

        if (renderBatch.AddedNamedValues is { } added)
        {
            var addedCount = added.Count;
            var addedArray = added.Array;
            for (var i = 0; i < addedCount; i++)
            {
                ref var addedEntry = ref addedArray[i];
                if (string.Equals(addedEntry.Name, OnSubmitNameAttribute, StringComparison.Ordinal) && addedEntry.Value is string assignedName)
                {
                    var location = (addedEntry.ComponentId, addedEntry.FrameIndex);
                    if (_namedEventsByAssignedName.TryAdd(assignedName, location))
                    {
                        _namedEventsByLocation.Add(location, assignedName);
                    }
                    else
                    {
                        // We could allow multiple events with the same name, since they are all tracked separately. However
                        // this is most likely a mistake on the developer's part so we will consider it an error.
                        var existingEntry = _namedEventsByAssignedName[assignedName];
                        throw new InvalidOperationException($"There is more than one named event with the name '{assignedName}'. Ensure named events have unique names. The following components both use this name: "
                            + $"\n - {GenerateComponentPath(existingEntry.ComponentId)}"
                            + $"\n - {GenerateComponentPath(addedEntry.ComponentId)}");
                    }
                }
            }
        }
    }

    private ulong FindEventHandlerIdForNamedEvent(int componentId, int frameIndex)
    {
        var frames = GetCurrentRenderTreeFrames(componentId);
        ref var frame = ref frames.Array[frameIndex];

        if (frame.FrameType != RenderTreeFrameType.NamedValue)
        {
            // This should not be possible, as the system doesn't create a way that the location could be wrong. But if it happens, we want to know.
            throw new InvalidOperationException($"The named value frame for component '{componentId}' at index '{frameIndex}' unexpectedly matches a frame of type '{frame.FrameType}'.");
        }

        if (!string.Equals(frame.NamedValueName, OnSubmitNameAttribute, StringComparison.Ordinal))
        {
            // This should not be possible, as currently we are only tracking name-values with the expected name. But if it happens, we want to know.
            throw new InvalidOperationException($"Expected a named value with name '{OnSubmitNameAttribute}' but found the name '{frame.NamedValueName}'.");
        }

        for (var i = frameIndex - 1; i >= 0; i--)
        {
            ref var candidate = ref frames.Array[i];
            if (candidate.FrameType == RenderTreeFrameType.Attribute)
            {
                if (candidate.AttributeEventHandlerId > 0 && string.Equals(candidate.AttributeName, "onsubmit", StringComparison.OrdinalIgnoreCase))
                {
                    return candidate.AttributeEventHandlerId;
                }
            }
            else if (candidate.FrameType == RenderTreeFrameType.Element)
            {
                break;
            }
        }

        // This won't be possible if the Razor compiler requires @onsubmit:name to be used only when there's an @onsubmit.
        throw new InvalidOperationException($"The {frame.NamedValueName} value in component {componentId} at index {frameIndex} does not match a preceding event handler.");
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
