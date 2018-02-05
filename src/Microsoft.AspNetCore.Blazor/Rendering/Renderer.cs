// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.AspNetCore.Blazor.Rendering
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

        // Because rendering is currently synchronous and single-threaded, we can keep re-using the
        // same RenderBatchBuilder instance to avoid reallocating
        private readonly RenderBatchBuilder _sharedRenderBatchBuilder = new RenderBatchBuilder();
        private int _renderBatchLock = 0;

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="Renderer"/>, assigning
        /// an identifier that is unique within the scope of the <see cref="Renderer"/>.
        /// </summary>
        /// <param name="component">The <see cref="IComponent"/>.</param>
        /// <returns>The assigned identifier for the <see cref="IComponent"/>.</returns>
        protected int AssignComponentId(IComponent component)
        {
            lock (_componentStateById)
            {
                var componentId = _nextComponentId++;
                var componentState = new ComponentState(this, componentId, component);
                _componentStateById.Add(componentId, componentState);
                _componentStateByComponent.Add(component, componentState); // Ensure the componentState lives for at least as long as the component
                return componentId;
            }
        }

        /// <summary>
        /// Updates the visible UI.
        /// </summary>
        /// <param name="renderBatch">The changes to the UI since the previous call.</param>
        internal protected abstract void UpdateDisplay(RenderBatch renderBatch);

        /// <summary>
        /// Updates the rendered state of the specified <see cref="IComponent"/>.
        /// </summary>
        /// <param name="componentId">The identifier of the <see cref="IComponent"/> to render.</param>
        protected internal void RenderNewBatch(int componentId)
        {
            // It's very important that components' rendering logic has no side-effects, and in particular
            // components must *not* trigger Render from inside their render logic, otherwise you could
            // easily get hard-to-debug infinite loops.
            // Since rendering is currently synchronous and single-threaded, we can enforce the above by
            // checking here that no other rendering process is already underway. This also means we only
            // need a single _renderBatchBuilder instance that can be reused throughout the lifetime of
            // the Renderer instance, which also means we're not allocating on a typical render cycle.
            // In the future, if rendering becomes async, we'll need a more sophisticated system of
            // capturing successive diffs from each component and probably serializing them for the
            // interop calls instead of using shared memory.

            // Note that Monitor.TryEnter is not yet supported in Mono WASM, so using the following instead
            var renderAlreadyRunning = Interlocked.CompareExchange(ref _renderBatchLock, 1, 0) == 1;
            if (renderAlreadyRunning)
            {
                throw new InvalidOperationException("Cannot render while a render is already in progress. " +
                    "Render logic must not have side-effects such as manually triggering other rendering.");
            }

            try
            {
                RenderInExistingBatch(_sharedRenderBatchBuilder, componentId);
                UpdateDisplay(_sharedRenderBatchBuilder.ToBatch());
            }
            finally
            {
                _sharedRenderBatchBuilder.Clear();
                Interlocked.Exchange(ref _renderBatchLock, 0);
            }            
        }

        internal void RenderInExistingBatch(RenderBatchBuilder batchBuilder, int componentId)
        {
            GetRequiredComponentState(componentId).Render(batchBuilder);
        }

        internal void DisposeInExistingBatch(RenderBatchBuilder batchBuilder, int componentId)
        {
            GetRequiredComponentState(componentId).NotifyDisposed();
            batchBuilder.AddDisposedComponent(componentId);
        }

        /// <summary>
        /// Notifies the specified component that an event has occurred.
        /// </summary>
        /// <param name="componentId">The unique identifier for the component within the scope of this <see cref="Renderer"/>.</param>
        /// <param name="renderTreeIndex">The index into the component's current render tree that specifies which event handler to invoke.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        protected void DispatchEvent(int componentId, int renderTreeIndex, UIEventArgs eventArgs)
            => GetRequiredComponentState(componentId).DispatchEvent(renderTreeIndex, eventArgs);

        internal void InstantiateChildComponent(RenderTreeFrame[] frames, int componentFrameIndex)
        {
            ref var frame = ref frames[componentFrameIndex];
            if (frame.FrameType != RenderTreeFrameType.Component)
            {
                throw new ArgumentException($"The frame's {nameof(RenderTreeFrame.FrameType)} property must equal {RenderTreeFrameType.Component}", nameof(frame));
            }

            if (frame.Component != null)
            {
                throw new ArgumentException($"The frame already has a non-null component instance", nameof(frame));
            }

            var newComponent = (IComponent)Activator.CreateInstance(frame.ComponentType);
            var newComponentId = AssignComponentId(newComponent);
            frame = frame.WithComponentInstance(newComponentId, newComponent);
        }

        private ComponentState GetRequiredComponentState(int componentId)
            => _componentStateById.TryGetValue(componentId, out var componentState)
                ? componentState
                : throw new ArgumentException($"The renderer does not have a component with ID {componentId}.");
    }
}
