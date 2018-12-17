// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering hierarchies of <see cref="IComponent"/> instances,
    /// dispatching events to them, and notifying when the user interface is being updated.
    /// </summary>
    public abstract class Renderer
    {
        private readonly ComponentFactory _componentFactory;
        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it
        private readonly Dictionary<int, ComponentState> _componentStateById
            = new Dictionary<int, ComponentState>();

        private readonly RenderBatchBuilder _batchBuilder = new RenderBatchBuilder();
        private bool _isBatchInProgress;

        private int _lastEventHandlerId = 0;
        private readonly Dictionary<int, EventHandlerInvoker> _eventBindings = new Dictionary<int, EventHandlerInvoker>();

        /// <summary>
        /// Constructs an instance of <see cref="Renderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initialising components.</param>
        public Renderer(IServiceProvider serviceProvider)
        {
            _componentFactory = new ComponentFactory(serviceProvider);
        }

        /// <summary>
        /// Constructs a new component of the specified type.
        /// </summary>
        /// <param name="componentType">The type of the component to instantiate.</param>
        /// <returns>The component instance.</returns>
        protected IComponent InstantiateComponent(Type componentType)
            => _componentFactory.InstantiateComponent(componentType);

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="Renderer"/>, assigning
        /// an identifier that is unique within the scope of the <see cref="Renderer"/>.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The component's assigned identifier.</returns>
        protected int AssignRootComponentId(IComponent component)
            => AttachAndInitComponent(component, -1).ComponentId;

        /// <summary>
        /// Gets the current render tree for a given component.
        /// </summary>
        /// <param name="componentId">The id for the component.</param>
        /// <returns>The <see cref="RenderTreeBuilder"/> representing the current render tree.</returns>
        private protected ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId) => GetRequiredComponentState(componentId).CurrrentRenderTree.GetFrames();

        /// <summary>
        /// Performs the first render for a root component. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        protected void RenderRootComponent(int componentId)
        {
            GetRequiredComponentState(componentId)
                .SetDirectParameters(ParameterCollection.Empty);
        }

        /// <summary>
        /// Performs the first render for a root component. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        /// <param name="initialParameters">The <see cref="ParameterCollection"/>with the initial parameters to use for rendering.</param>
        protected void RenderRootComponent(int componentId, ParameterCollection initialParameters)
        {
            GetRequiredComponentState(componentId)
                .SetDirectParameters(initialParameters);
        }

        private ComponentState AttachAndInitComponent(IComponent component, int parentComponentId)
        {
            var componentId = _nextComponentId++;
            var parentComponentState = GetOptionalComponentState(parentComponentId);
            var componentState = new ComponentState(this, componentId, component, parentComponentState);
            _componentStateById.Add(componentId, componentState);
            component.Init(new RenderHandle(this, componentId));
            return componentState;
        }

        /// <summary>
        /// Updates the visible UI.
        /// </summary>
        /// <param name="renderBatch">The changes to the UI since the previous call.</param>
        /// <returns>A <see cref="Task"/> to represent the UI update process.</returns>
        protected abstract Task UpdateDisplayAsync(in RenderBatch renderBatch);

        /// <summary>
        /// Notifies the specified component that an event has occurred.
        /// </summary>
        /// <param name="componentId">The unique identifier for the component within the scope of this <see cref="Renderer"/>.</param>
        /// <param name="eventHandlerId">The <see cref="RenderTreeFrame.AttributeEventHandlerId"/> value from the original event attribute.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        public void DispatchEvent(int componentId, int eventHandlerId, UIEventArgs eventArgs)
        {
            if (_eventBindings.TryGetValue(eventHandlerId, out var binding))
            {
                // The event handler might request multiple renders in sequence. Capture them
                // all in a single batch.
                try
                {
                    _isBatchInProgress = true;
                    GetRequiredComponentState(componentId).DispatchEvent(binding, eventArgs);
                }
                finally
                {
                    _isBatchInProgress = false;
                    ProcessRenderQueue();
                }
            }
            else
            {
                throw new ArgumentException($"There is no event handler with ID {eventHandlerId}");
            }
        }

        internal void InstantiateChildComponentOnFrame(ref RenderTreeFrame frame, int parentComponentId)
        {
            if (frame.FrameType != RenderTreeFrameType.Component)
            {
                throw new ArgumentException($"The frame's {nameof(RenderTreeFrame.FrameType)} property must equal {RenderTreeFrameType.Component}", nameof(frame));
            }

            if (frame.ComponentState != null)
            {
                throw new ArgumentException($"The frame already has a non-null component instance", nameof(frame));
            }

            var newComponent = InstantiateComponent(frame.ComponentType);
            var newComponentState = AttachAndInitComponent(newComponent, parentComponentId);
            frame = frame.WithComponent(newComponentState);
        }

        internal void AssignEventHandlerId(ref RenderTreeFrame frame)
        {
            var id = ++_lastEventHandlerId;

            if (frame.AttributeValue is MulticastDelegate @delegate)
            {
               _eventBindings.Add(id, new EventHandlerInvoker(@delegate));
            }

            frame = frame.WithAttributeEventHandlerId(id);
        }

        /// <summary>
        /// Schedules a render for the specified <paramref name="componentId"/>. Its display
        /// will be populated using the specified <paramref name="renderFragment"/>.
        /// </summary>
        /// <param name="componentId">The ID of the component to render.</param>
        /// <param name="renderFragment">A <see cref="RenderFragment"/> that will supply the updated UI contents.</param>
        protected internal virtual void AddToRenderQueue(int componentId, RenderFragment renderFragment)
        {
            var componentState = GetOptionalComponentState(componentId);
            if (componentState == null)
            {
                // If the component was already disposed, then its render handle trying to
                // queue a render is a no-op.
                return;
            }

            _batchBuilder.ComponentRenderQueue.Enqueue(
                new RenderQueueEntry(componentState, renderFragment));

            if (!_isBatchInProgress)
            {
                ProcessRenderQueue();
            }
        }

        private ComponentState GetRequiredComponentState(int componentId)
            => _componentStateById.TryGetValue(componentId, out var componentState)
                ? componentState
                : throw new ArgumentException($"The renderer does not have a component with ID {componentId}.");

        private ComponentState GetOptionalComponentState(int componentId)
            => _componentStateById.TryGetValue(componentId, out var componentState)
                ? componentState
                : null;

        private void ProcessRenderQueue()
        {
            _isBatchInProgress = true;
            var updateDisplayTask = Task.CompletedTask;

            try
            {
                // Process render queue until empty
                while (_batchBuilder.ComponentRenderQueue.Count > 0)
                {
                    var nextToRender = _batchBuilder.ComponentRenderQueue.Dequeue();
                    RenderInExistingBatch(nextToRender);
                }

                var batch = _batchBuilder.ToBatch();
                updateDisplayTask = UpdateDisplayAsync(batch);
                InvokeRenderCompletedCalls(batch.UpdatedComponents);
            }
            finally
            {
                RemoveEventHandlerIds(_batchBuilder.DisposedEventHandlerIds.ToRange(), updateDisplayTask);
                _batchBuilder.Clear();
                _isBatchInProgress = false;
            }
        }

        private void InvokeRenderCompletedCalls(ArrayRange<RenderTreeDiff> updatedComponents)
        {
            var array = updatedComponents.Array;
            for (var i = 0; i < updatedComponents.Count; i++)
            {
                // The component might be rendered and disposed in the same batch (if its parent
                // was rendered later in the batch, and removed the child from the tree).
                GetOptionalComponentState(array[i].ComponentId)?.NotifyRenderCompleted();
            }
        }

        private void RenderInExistingBatch(RenderQueueEntry renderQueueEntry)
        {
            renderQueueEntry.ComponentState
                .RenderIntoBatch(_batchBuilder, renderQueueEntry.RenderFragment);

            // Process disposal queue now in case it causes further component renders to be enqueued
            while (_batchBuilder.ComponentDisposalQueue.Count > 0)
            {
                var disposeComponentId = _batchBuilder.ComponentDisposalQueue.Dequeue();
                GetRequiredComponentState(disposeComponentId).DisposeInBatch(_batchBuilder);
                _componentStateById.Remove(disposeComponentId);
                _batchBuilder.DisposedComponentIds.Append(disposeComponentId);
            }
        }

        private void RemoveEventHandlerIds(ArrayRange<int> eventHandlerIds, Task afterTask)
        {
            if (eventHandlerIds.Count == 0)
            {
                return;
            }

            if (afterTask.IsCompleted)
            {
                var array = eventHandlerIds.Array;
                var count = eventHandlerIds.Count;
                for (var i = 0; i < count; i++)
                {
                    _eventBindings.Remove(array[i]);
                }
            }
            else
            {
                // We need to delay the actual removal (e.g., until we've confirmed the client
                // has processed the batch and hence can be sure not to reuse the handler IDs
                // any further). We must clone the data because the underlying RenderBatchBuilder
                // may be reused and hence modified by an unrelated subsequent batch.
                var eventHandlerIdsClone = eventHandlerIds.Clone();
                afterTask.ContinueWith(_ =>
                    RemoveEventHandlerIds(eventHandlerIdsClone, Task.CompletedTask));
            }
        }
    }
}
