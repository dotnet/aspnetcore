// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Blazor.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering hierarchies of <see cref="IComponent"/> instances,
    /// dispatching events to them, and notifying when the user interface is being updated.
    /// </summary>
    public abstract class Renderer
    {
        // Methods for tracking associations between component IDs, instances, and states,
        // without pinning any of them in memory here. The explictly GC rooted items are the
        // components explicitly added to the renderer (i.e., top-level components). In turn
        // these reference descendant components and associated ComponentState instances.
        private readonly WeakValueDictionary<int, ComponentState> _componentStateById
            = new WeakValueDictionary<int, ComponentState>();
        private readonly ConditionalWeakTable<IComponent, ComponentState> _componentStateByComponent
            = new ConditionalWeakTable<IComponent, ComponentState>();
        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it

        // Ensure that explictly-added components (and transitively, their current descendants)
        // aren't GCed. If we don't do this then the display layer (e.g., browser) might send
        // us events for component IDs where the corresponding state has already been collected.
        private readonly IList<IComponent> _topLevelComponents = new List<IComponent>();

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="Renderer"/>, assigning
        /// an identifier that is unique within the scope of the <see cref="Renderer"/>.
        /// </summary>
        /// <param name="component">The <see cref="IComponent"/>.</param>
        /// <returns>The assigned identifier for the <see cref="IComponent"/>.</returns>
        protected internal int AssignComponentId(IComponent component)
        {
            lock (_componentStateByComponent)
            {
                if (_componentStateByComponent.TryGetValue(component, out _))
                {
                    throw new ArgumentException("The component was already associated with the renderer.");
                }

                var componentId = _nextComponentId++;
                var componentState = new ComponentState(this, componentId, component);
                _componentStateById.Add(componentId, componentState);
                _componentStateByComponent.Add(component, componentState);
                return componentId;
            }
        }

        /// <summary>
        /// Updates the visible UI to display the supplied <paramref name="renderTree"/>
        /// at the location corresponding to the <paramref name="componentId"/>.
        /// </summary>
        /// <param name="componentId">The identifier for the updated <see cref="IComponent"/>.</param>
        /// <param name="renderTree">The updated render tree to be displayed.</param>
        internal protected abstract void UpdateDisplay(int componentId, ArraySegment<RenderTreeNode> renderTree);

        /// <summary>
        /// Updates the rendered state of the specified <see cref="IComponent"/>.
        /// </summary>
        /// <param name="component">The <see cref="IComponent"/>.</param>
        protected void RenderComponent(IComponent component)
            => GetRequiredComponentState(component).Render();

        /// <summary>
        /// Updates the rendered state of the specified <see cref="IComponent"/>.
        /// </summary>
        /// <param name="componentId">The identifier of the <see cref="IComponent"/> to render.</param>
        protected void RenderComponent(int componentId)
            => GetRequiredComponentState(componentId).Render();

        /// <summary>
        /// Notifies the specified component that an event has occurred.
        /// </summary>
        /// <param name="componentId">The unique identifier for the component within the scope of this <see cref="Renderer"/>.</param>
        /// <param name="renderTreeIndex">The index into the component's current render tree that specifies which event handler to invoke.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        protected void DispatchEvent(int componentId, int renderTreeIndex, UIEventArgs eventArgs)
            => GetRequiredComponentState(componentId).DispatchEvent(renderTreeIndex, eventArgs);

        private ComponentState GetRequiredComponentState(int componentId)
            => _componentStateById.TryGetValue(componentId, out var componentState)
                ? componentState
                : throw new ArgumentException($"The renderer does not have a component with ID {componentId}.");

        private ComponentState GetRequiredComponentState(IComponent component)
            => _componentStateByComponent.TryGetValue(component, out var componentState)
                ? componentState
                : throw new ArgumentException("The component is not associated with the renderer.");
    }
}
