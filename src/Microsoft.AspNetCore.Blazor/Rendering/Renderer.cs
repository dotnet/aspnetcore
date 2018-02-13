// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering hierarchies of <see cref="IComponent"/> instances,
    /// dispatching events to them, and notifying when the user interface is being updated.
    /// </summary>
    public abstract class Renderer
    {
        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it
        private readonly Dictionary<int, ComponentState> _componentStateById
            = new Dictionary<int, ComponentState>();

        // Because rendering is currently synchronous and single-threaded, we can keep re-using the
        // same RenderBatchBuilder instance to avoid reallocating
        private readonly RenderBatchBuilder _sharedRenderBatchBuilder = new RenderBatchBuilder();
        private int _renderBatchLock = 0;

        private int _lastEventHandlerId = 0;
        private readonly Dictionary<int, UIEventHandler> _eventHandlersById
            = new Dictionary<int, UIEventHandler>();

        /// <summary>
        /// Constructs a new component of the specified type.
        /// </summary>
        /// <param name="componentType">The type of the component to instantiate.</param>
        /// <returns>The component instance.</returns>
        protected IComponent InstantiateComponent(Type componentType)
        {
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"Must implement {nameof(IComponent)}", nameof(componentType));
            }

            return (IComponent)Activator.CreateInstance(componentType);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="Renderer"/>, assigning
        /// an identifier that is unique within the scope of the <see cref="Renderer"/>.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The component's assigned identifier.</returns>
        protected int AssignComponentId(IComponent component)
        {
            var componentId = _nextComponentId++;
            var componentState = new ComponentState(this, componentId, component);
            _componentStateById.Add(componentId, componentState);
            component.Init(new RenderHandle(this, componentId));
            return componentId;
        }

        /// <summary>
        /// Updates the visible UI.
        /// </summary>
        /// <param name="renderBatch">The changes to the UI since the previous call.</param>
        protected abstract void UpdateDisplay(RenderBatch renderBatch);

        /// <summary>
        /// Updates the rendered state of the specified <see cref="IComponent"/>.
        /// </summary>
        /// <param name="componentId">The identifier of the <see cref="IComponent"/> to render.</param>
        protected void RenderNewBatch(int componentId)
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

            _sharedRenderBatchBuilder.ComponentRenderQueue.Enqueue(componentId);

            try
            {
                // Process render queue until empty
                while (_sharedRenderBatchBuilder.ComponentRenderQueue.Count > 0)
                {
                    var nextComponentIdToRender = _sharedRenderBatchBuilder.ComponentRenderQueue.Dequeue();
                    RenderInExistingBatch(_sharedRenderBatchBuilder, nextComponentIdToRender);
                }

                UpdateDisplay(_sharedRenderBatchBuilder.ToBatch());
            }
            finally
            {
                RemoveEventHandlerIds(_sharedRenderBatchBuilder.DisposedEventHandlerIds.ToRange());
                _sharedRenderBatchBuilder.Clear();
                Interlocked.Exchange(ref _renderBatchLock, 0);
            }            
        }

        /// <summary>
        /// Notifies the specified component that an event has occurred.
        /// </summary>
        /// <param name="componentId">The unique identifier for the component within the scope of this <see cref="Renderer"/>.</param>
        /// <param name="eventHandlerId">The <see cref="RenderTreeFrame.AttributeEventHandlerId"/> value from the original event attribute.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        protected void DispatchEvent(int componentId, int eventHandlerId, UIEventArgs eventArgs)
        {
            if (_eventHandlersById.TryGetValue(eventHandlerId, out var handler))
            {
                GetRequiredComponentState(componentId).DispatchEvent(handler, eventArgs);
            }
            else
            {
                throw new ArgumentException($"There is no event handler with ID {eventHandlerId}");
            }
        }

        internal void InstantiateChildComponentOnFrame(ref RenderTreeFrame frame)
        {
            if (frame.FrameType != RenderTreeFrameType.Component)
            {
                throw new ArgumentException($"The frame's {nameof(RenderTreeFrame.FrameType)} property must equal {RenderTreeFrameType.Component}", nameof(frame));
            }

            if (frame.Component != null)
            {
                throw new ArgumentException($"The frame already has a non-null component instance", nameof(frame));
            }

            var newComponent = InstantiateComponent(frame.ComponentType);
            var newComponentId = AssignComponentId(newComponent);
            frame = frame.WithComponentInstance(newComponentId, newComponent);
        }

        internal void AssignEventHandlerId(ref RenderTreeFrame frame)
        {
            var id = ++_lastEventHandlerId;
            _eventHandlersById.Add(id, (UIEventHandler)frame.AttributeValue);
            frame = frame.WithAttributeEventHandlerId(id);
        }

        internal void ComponentRequestedRender(int componentId)
        {
            // TODO: Clean up the locking around rendering. The Renderer doesn't really need
            // to be thread-safe, and the following code isn't actually thread-safe anyway.
            if (_renderBatchLock == 0)
            {
                RenderNewBatch(componentId);
            }
            else
            {
                _sharedRenderBatchBuilder.ComponentRenderQueue.Enqueue(componentId);
            }
        }

        private ComponentState GetRequiredComponentState(int componentId)
            => _componentStateById.TryGetValue(componentId, out var componentState)
                ? componentState
                : throw new ArgumentException($"The renderer does not have a component with ID {componentId}.");

        private void RenderInExistingBatch(RenderBatchBuilder batchBuilder, int componentId)
        {
            GetRequiredComponentState(componentId).RenderIntoBatch(batchBuilder);

            // Process disposal queue now in case it causes further component renders to be enqueued
            while (batchBuilder.ComponentDisposalQueue.Count > 0)
            {
                var disposeComponentId = batchBuilder.ComponentDisposalQueue.Dequeue();
                GetRequiredComponentState(disposeComponentId).DisposeInBatch(batchBuilder);
                _componentStateById.Remove(disposeComponentId);
                batchBuilder.DisposedComponentIds.Append(disposeComponentId);
            }
        }

        private void RemoveEventHandlerIds(ArrayRange<int> eventHandlerIds)
        {
            var array = eventHandlerIds.Array;
            var count = eventHandlerIds.Count;
            for (var i = 0; i < count; i++)
            {
                _eventHandlersById.Remove(array[i]);
            }
        }
    }
}
