// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering hierarchies of <see cref="IComponent"/> instances,
    /// dispatching events to them, and notifying when the user interface is being updated.
    /// </summary>
    public abstract class Renderer : IDisposable
    {
        private readonly ComponentFactory _componentFactory;
        private readonly Dictionary<int, ComponentState> _componentStateById = new Dictionary<int, ComponentState>();
        private readonly RenderBatchBuilder _batchBuilder = new RenderBatchBuilder();
        private readonly Dictionary<int, EventHandlerInvoker> _eventBindings = new Dictionary<int, EventHandlerInvoker>();

        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it
        private bool _isBatchInProgress;
        private int _lastEventHandlerId = 0;
        private List<Task> _pendingTasks;

        // We need to introduce locking as we don't know if we are executing
        // under a synchronization context that limits the ammount of concurrency
        // that can happen when async callbacks are executed.
        // As a result, we have to protect the _pendingTask list and the
        // _batchBuilder render queue from concurrent modifications.
        private object _asyncWorkLock = new object();

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
            RenderRootComponent(componentId, ParameterCollection.Empty);
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
            ReportAsyncExceptions(RenderRootComponentAsync(componentId, initialParameters));
        }

        private async void ReportAsyncExceptions(Task task)
        {
            switch (task.Status)
            {
                // If it's already completed synchronously, no need to await and no
                // need to issue a further render (we already rerender synchronously).
                // Just need to make sure we propagate any errors.
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                    _pendingTasks = null;
                    break;
                case TaskStatus.Faulted:
                    _pendingTasks = null;
                    HandleException(task.Exception);
                    break;

                default:
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        // Either the task failed, or it was cancelled.
                        // We want to report task failure exceptions only.
                        if (!task.IsCanceled)
                        {
                            HandleException(ex);
                        }
                    }
                    finally
                    {
                        // Clear the list after we are done rendering the root component or an async exception has ocurred.
                        _pendingTasks = null;
                    }

                    break;
            }
        }

        private static void HandleException(Exception ex)
        {
            if (ex is AggregateException && ex.InnerException != null)
            {
                ex = ex.InnerException; // It's more useful
            }

            // TODO: Need better global exception handling
            Console.Error.WriteLine($"[{ex.GetType().FullName}] {ex.Message}\n{ex.StackTrace}");
        }

        /// <summary>
        /// Performs the first render for a root component, waiting for this component and all
        /// children components to finish rendering in case there is any asynchronous work being
        /// done by any of the components. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        protected Task RenderRootComponentAsync(int componentId)
        {
            return RenderRootComponentAsync(componentId, ParameterCollection.Empty);
        }

        /// <summary>
        /// Performs the first render for a root component, waiting for this component and all
        /// children components to finish rendering in case there is any asynchronous work being
        /// done by any of the components. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        /// <param name="initialParameters">The <see cref="ParameterCollection"/>with the initial parameters to use for rendering.</param>
        protected async Task RenderRootComponentAsync(int componentId, ParameterCollection initialParameters)
        {
            if (_pendingTasks != null)
            {
                throw new InvalidOperationException("There is an ongoing rendering in progress.");
            }
            _pendingTasks = new List<Task>();
            // During the rendering process we keep a list of components performing work in _pendingTasks.
            // _renderer.AddToPendingTasks will be called by ComponentState.SetDirectParameters to add the
            // the Task produced by Component.SetParametersAsync to _pendingTasks in order to track the
            // remaining work.
            // During the synchronous rendering process we don't wait for the pending asynchronous
            // work to finish as it will simply trigger new renders that will be handled afterwards.
            // During the asynchronous rendering process we want to wait up untill al components have
            // finished rendering so that we can produce the complete output.
            GetRequiredComponentState(componentId)
                .SetDirectParameters(initialParameters);

            try
            {
                await ProcessAsynchronousWork();
                Debug.Assert(_pendingTasks.Count == 0);
            }
            finally
            {
                _pendingTasks = null;
            }
        }

        private async Task ProcessAsynchronousWork()
        {
            // Child components SetParametersAsync are stored in the queue of pending tasks,
            // which might trigger further renders.
            while (_pendingTasks.Count > 0)
            {
                Task pendingWork;
                lock (_asyncWorkLock)
                {
                    // Create a Task that represents the remaining ongoing work for the rendering process
                    pendingWork = Task.WhenAll(_pendingTasks);

                    // Clear all pending work.
                    _pendingTasks.Clear();
                }

                // new work might be added before we check again as a result of waiting for all
                // the child components to finish executing SetParametersAsync
                await pendingWork;
            };
        }

        private ComponentState AttachAndInitComponent(IComponent component, int parentComponentId)
        {
            var componentId = _nextComponentId++;
            var parentComponentState = GetOptionalComponentState(parentComponentId);
            var componentState = new ComponentState(this, componentId, component, parentComponentState);
            _componentStateById.Add(componentId, componentState);
            component.Configure(new RenderHandle(this, componentId));
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

        /// <summary>
        /// Executes the supplied work item on the renderer's
        /// synchronization context.
        /// </summary>
        /// <param name="workItem">The work item to execute.</param>
        public virtual Task Invoke(Action workItem)
        {
            // Base renderer has nothing to dispatch to, so execute directly
            workItem();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes the supplied work item on the renderer's
        /// synchronization context.
        /// </summary>
        /// <param name="workItem">The work item to execute.</param>
        public virtual Task InvokeAsync(Func<Task> workItem)
        {
            // Base renderer has nothing to dispatch to, so execute directly
            return workItem();
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

        internal void AddToPendingTasks(Task task)
        {
            switch (task == null ? TaskStatus.RanToCompletion : task.Status)
            {
                // If it's already completed synchronously, no need to add it to the list of
                // pending Tasks as no further render (we already rerender synchronously) will.
                // happen.
                case TaskStatus.RanToCompletion:
                case TaskStatus.Canceled:
                    break;
                case TaskStatus.Faulted:
                    // We want to throw immediately if the task failed synchronously instead of
                    // waiting for it to throw later. This can happen if the task is produced by
                    // an 'async' state machine (the ones generated using async/await) where even
                    // the synchronous exceptions will get captured and converted into a faulted
                    // task.
                    ExceptionDispatchInfo.Capture(task.Exception.InnerException).Throw();
                    break;
                default:
                    // We are not in rendering the root component.
                    if (_pendingTasks == null)
                    {
                        return;
                    }
                    lock (_asyncWorkLock)
                    {
                        _pendingTasks.Add(task);
                    }
                    break;
            }
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

            lock (_asyncWorkLock)
            {
                _batchBuilder.ComponentRenderQueue.Enqueue(
                    new RenderQueueEntry(componentState, renderFragment));
            }

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
                while (TryDequeueRenderQueueEntry(out var nextToRender))
                {
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

        private bool TryDequeueRenderQueueEntry(out RenderQueueEntry entry)
        {
            lock (_asyncWorkLock)
            {
                if (_batchBuilder.ComponentRenderQueue.Count > 0)
                {
                    entry = _batchBuilder.ComponentRenderQueue.Dequeue();
                    return true;
                }
                else
                {
                    entry = default;
                    return false;
                }
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

        /// <summary>
        /// Releases all resources currently used by this <see cref="Renderer"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this method is being invoked by <see cref="IDisposable.Dispose"/>, otherwise <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            List<Exception> exceptions = null;

            foreach (var componentState in _componentStateById.Values)
            {
                if (componentState.Component is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception exception)
                    {
                        // Capture exceptions thrown by individual components and rethrow as an aggregate.
                        exceptions = exceptions ?? new List<Exception>();
                        exceptions.Add(exception);
                    }
                }
            }

            if (exceptions != null)
            {
                throw new AggregateException(exceptions);
            }
        }

        /// <summary>
        /// Releases all resources currently used by this <see cref="Renderer"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
