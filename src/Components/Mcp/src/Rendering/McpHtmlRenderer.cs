// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.HtmlRendering.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Mcp.Rendering;

internal sealed class McpHtmlRenderer(IServiceProvider services, ILoggerFactory loggerFactory)
    : StaticHtmlRenderer(services, loggerFactory)
{
    private readonly Dictionary<(int ComponentId, int FrameIndex), string> _namesByLocation = new();
    private readonly Dictionary<string, HashSet<(int ComponentId, int FrameIndex)>> _locationsByName = new(StringComparer.Ordinal);

    public Task DispatchSubmitAsync(string handlerName)
    {
        Dispatcher.AssertAccess();

        if (string.IsNullOrEmpty(handlerName))
        {
            throw new McpFormSubmissionException("The submission does not identify a named form. Specify @formname or EditForm.FormName.");
        }

        if (!_locationsByName.TryGetValue(handlerName, out var locations) || locations.Count == 0)
        {
            throw new McpFormSubmissionException($"Cannot submit the form '{handlerName}' because no form on the page currently has that name.");
        }

        if (locations.Count > 1)
        {
            var message = new StringBuilder($"There is more than one named submit event with the name '{handlerName}'. Ensure named submit events have unique names, or are in scopes with distinct names. The following components use this name:");
            foreach (var location in locations)
            {
                var path = new Stack<string>();
                for (var state = GetComponentState(location.ComponentId); state is not null; state = state.ParentComponentState)
                {
                    path.Push(state.Component.GetType().Name);
                }

                message.Append(CultureInfo.InvariantCulture, $"\n - {string.Join(" > ", path)}");
            }

            throw new InvalidOperationException(message.ToString());
        }

        var (componentId, frameIndex) = locations.Single();
        var frames = GetCurrentRenderTreeFrames(componentId);
        ref var frame = ref frames.Array[frameIndex];
        if (frame.FrameType != RenderTreeFrameType.NamedEvent || frame.NamedEventType != "onsubmit")
        {
            throw new InvalidOperationException("The named form no longer corresponds to its tracked render-tree frame.");
        }

        for (var index = frameIndex - 1; index >= 0; index--)
        {
            ref var candidate = ref frames.Array[index];
            if (candidate.FrameType == RenderTreeFrameType.Element)
            {
                break;
            }

            if (candidate.FrameType == RenderTreeFrameType.Attribute
                && candidate.AttributeEventHandlerId > 0
                && string.Equals(candidate.AttributeName, "onsubmit", StringComparison.OrdinalIgnoreCase))
            {
                return DispatchEventAsync(candidate.AttributeEventHandlerId, null, EventArgs.Empty, waitForQuiescence: true);
            }
        }

        return Task.CompletedTask;
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        if (renderBatch.NamedEventChanges is { } changes)
        {
            for (var index = 0; index < changes.Count; index++)
            {
                ref var change = ref changes.Array[index];
                if (change.ChangeType == NamedEventChangeType.Removed
                    && change.EventType == "onsubmit"
                    && _namesByLocation.Remove((change.ComponentId, change.FrameIndex), out var name))
                {
                    var locations = _locationsByName[name];
                    locations.Remove((change.ComponentId, change.FrameIndex));
                    if (locations.Count == 0)
                    {
                        _locationsByName.Remove(name);
                    }
                }
            }

            for (var index = 0; index < changes.Count; index++)
            {
                ref var change = ref changes.Array[index];
                if (change.ChangeType == NamedEventChangeType.Added
                    && change.EventType == "onsubmit"
                    && TryCreateScopeQualifiedEventName(change.ComponentId, change.AssignedName, out var name))
                {
                    if (!_locationsByName.TryGetValue(name, out var locations))
                    {
                        locations = new();
                        _locationsByName.Add(name, locations);
                    }

                    var location = (change.ComponentId, change.FrameIndex);
                    if (!locations.Add(location))
                    {
                        throw new InvalidOperationException("A named submit event was added twice at the same render-tree location.");
                    }

                    _namesByLocation.Add(location, name);
                }
            }
        }

        return base.UpdateDisplayAsync(renderBatch);
    }

    protected override IComponent ResolveComponentForRenderMode(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        int? parentComponentId,
        IComponentActivator componentActivator,
        IComponentRenderMode renderMode)
        => throw new NotSupportedException($"The MCP static rendering host does not support the interactive render mode '{renderMode.GetType().Name}' on '{componentType}'.");
}
