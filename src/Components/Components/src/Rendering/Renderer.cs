// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        private readonly Dictionary<int, EventCallback> _eventBindings = new Dictionary<int, EventCallback>();
        private readonly IDispatcher _dispatcher;

        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it
        private bool _isBatchInProgress;
        private int _lastEventHandlerId = 0;
        private List<Task> _pendingTasks;

        /// <summary>
        /// Allows the caller to handle exceptions from the SynchronizationContext when one is available.
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledSynchronizationException
        {
            add
            {
                if (!(_dispatcher is RendererSynchronizationContext rendererSynchronizationContext))
                {
                    return;
                }
                rendererSynchronizationContext.UnhandledException += value;
            }
            remove
            {
                if (!(_dispatcher is RendererSynchronizationContext rendererSynchronizationContext))
                {
                    return;
                }
                rendererSynchronizationContext.UnhandledException -= value;
            }
        }

        /// <summary>
        /// Constructs an instance of <see cref="Renderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initializing components.</param>
        public Renderer(IServiceProvider serviceProvider)
        {
            _componentFactory = new ComponentFactory(serviceProvider);
        }

        /// <summary>
        /// Constructs an instance of <see cref="Renderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initializing components.</param>
        /// <param name="dispatcher">The <see cref="IDispatcher"/> to be for invoking user actions into the <see cref="Renderer"/> context.</param>
        public Renderer(IServiceProvider serviceProvider, IDispatcher dispatcher) : this(serviceProvider)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Creates an <see cref="IDispatcher"/> that can be used with one or more <see cref="Renderer"/>.
        /// </summary>
        /// <returns>The <see cref="IDispatcher"/>.</returns>
        public static IDispatcher CreateDefaultDispatcher() => new RendererSynchronizationContext();

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
        // Internal for unit testing
        protected internal int AssignRootComponentId(IComponent component)
            => AttachAndInitComponent(component, -1).ComponentId;

        /// <summary>
        /// Gets the current render tree for a given component.
        /// </summary>
        /// <param name="componentId">The id for the component.</param>
        /// <returns>The <see cref="RenderTreeBuilder"/> representing the current render tree.</returns>
        private protected ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId) => GetRequiredComponentState(componentId).CurrrentRenderTree.GetFrames();

        /// <summary>
        /// Performs the first render for a root component, waiting for this component and all
        /// children components to finish rendering in case there is any asynchronous work being
        /// done by any of the components. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        /// <remarks>
        /// Rendering a root component is an asynchronous operation. Clients may choose to not await the returned task to
        /// start, but not wait for the entire render to complete.
        /// </remarks>
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
        /// <remarks>
        /// Rendering a root component is an asynchronous operation. Clients may choose to not await the returned task to
        /// start, but not wait for the entire render to complete.
        /// </remarks>
        protected async Task RenderRootComponentAsync(int componentId, ParameterCollection initialParameters)
        {
            if (Interlocked.CompareExchange(ref _pendingTasks, new List<Task>(), null) != null)
            {
                throw new InvalidOperationException("There is an ongoing rendering in progress.");
            }

            // During the rendering process we keep a list of components performing work in _pendingTasks.
            // _renderer.AddToPendingTasks will be called by ComponentState.SetDirectParameters to add the
            // the Task produced by Component.SetParametersAsync to _pendingTasks in order to track the
            // remaining work.
            // During the synchronous rendering process we don't wait for the pending asynchronous
            // work to finish as it will simply trigger new renders that will be handled afterwards.
            // During the asynchronous rendering process we want to wait up untill al components have
            // finished rendering so that we can produce the complete output.
            var componentState = GetRequiredComponentState(componentId);
            componentState.SetDirectParameters(initialParameters);

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

        /// <summary>
        /// Allows derived types to handle exceptions during rendering. Defaults to rethrowing the original exception.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        protected abstract void HandleException(Exception exception);

        private async Task ProcessAsynchronousWork()
        {
            // Child components SetParametersAsync are stored in the queue of pending tasks,
            // which might trigger further renders.
            while (_pendingTasks.Count > 0)
            {
                // Create a Task that represents the remaining ongoing work for the rendering process
                var pendingWork = Task.WhenAll(_pendingTasks);

                // Clear all pending work.
                _pendingTasks.Clear();

                // new work might be added before we check again as a result of waiting for all
                // the child components to finish executing SetParametersAsync
                await pendingWork;
            }
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
        /// Notifies the renderer that an event has occurred.
        /// </summary>
        /// <param name="eventHandlerId">The <see cref="RenderTreeFrame.AttributeEventHandlerId"/> value from the original event attribute.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        /// <returns>
        /// A <see cref="Task"/> which will complete once all asynchronous processing related to the event
        /// has completed.
        /// </returns>
        public Task DispatchEventAsync(int eventHandlerId, UIEventArgs eventArgs)
        {
            EnsureSynchronizationContext();

            if (!_eventBindings.TryGetValue(eventHandlerId, out var callback))
            {
                throw new ArgumentException($"There is no event handler with ID {eventHandlerId}");
            }

            Task task = null;
            try
            {
                // The event handler might request multiple renders in sequence. Capture them
                // all in a single batch.
                _isBatchInProgress = true;

                task = callback.InvokeAsync(eventArgs);
            }
            finally
            {
                _isBatchInProgress = false;

                // Since the task has yielded - process any queued rendering work before we return control
                // to the caller.
                ProcessRenderQueue();
            }

            // Task completed synchronously or is still running. We already processed all of the rendering
            // work that was queued so let our error handler deal with it.
            return GetErrorHandledTask(task);
        }

        /// <summary>
        /// Executes the supplied work item on the renderer's
        /// synchronization context.
        /// </summary>
        /// <param name="workItem">The work item to execute.</param>
        public virtual Task Invoke(Action workItem)
        {
            // This is for example when we run on a system with a single thread, like WebAssembly.
            if (_dispatcher == null)
            {
                workItem();
                return Task.CompletedTask;
            }

            if (SynchronizationContext.Current == _dispatcher)
            {
                // This is an optimization for when the dispatcher is also a syncronization context, like in the default case.
                // No need to dispatch. Avoid deadlock by invoking directly.
                workItem();
                return Task.CompletedTask;
            }
            else
            {
                return _dispatcher.Invoke(workItem);
            }
        }

        /// <summary>
        /// Executes the supplied work item on the renderer's
        /// synchronization context.
        /// </summary>
        /// <param name="workItem">The work item to execute.</param>
        public virtual Task InvokeAsync(Func<Task> workItem)
        {
            // This is for example when we run on a system with a single thread, like WebAssembly.
            if (_dispatcher == null)
            {
                return workItem();
            }

            if (SynchronizationContext.Current == _dispatcher)
            {
                // This is an optimization for when the dispatcher is also a syncronization context, like in the default case.
                // No need to dispatch. Avoid deadlock by invoking directly.
                return workItem();
            }
            else
            {
                return _dispatcher.InvokeAsync(workItem);
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
                    // We want to immediately handle exceptions if the task failed synchronously instead of
                    // waiting for it to throw later. This can happen if the task is produced by
                    // an 'async' state machine (the ones generated using async/await) where even
                    // the synchronous exceptions will get captured and converted into a faulted
                    // task.
                    HandleException(task.Exception.GetBaseException());
                    break;
                default:
                    // It's important to evaluate the following even if we're not going to use
                    // handledErrorTask below, because it has the side-effect of calling HandleException.
                    var handledErrorTask = GetErrorHandledTask(task);

                    // The pendingTasks collection is only used during prerendering to track quiescence,
                    // so will be null at other times.
                    _pendingTasks?.Add(handledErrorTask);
                    
                    break;
            }
        }

        internal void AssignEventHandlerId(ref RenderTreeFrame frame)
        {
            var id = ++_lastEventHandlerId;

            if (frame.AttributeValue is EventCallback callback)
            {
                // We hit this case when a EventCallback object is produced that needs an explicit receiver.
                // Common cases for this are "chained bind" or "chained event handler" when a component
                // accepts a delegate as a parameter and then hooks it up to a DOM event.
                //
                // When that happens we intentionally box the EventCallback because we need to hold on to
                // the receiver.
                _eventBindings.Add(id, callback);
            }
            else if (frame.AttributeValue is MulticastDelegate @delegate)
            {
                // This is the common case for a delegate, where the receiver of the event
                // is the same as delegate.Target. In this case since the receiver is implicit we can
                // avoid boxing the EventCallback object and just re-hydrate it on the other side of the
                // render tree.
                _eventBindings.Add(id, new EventCallback(@delegate.Target as IHandleEvent, @delegate));
            }

            // NOTE: we do not to handle EventCallback<T> here. EventCallback<T> is only used when passing
            // a callback to a component, and never when used to attaching a DOM event handler.

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
            EnsureSynchronizationContext();

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

        private void EnsureSynchronizationContext()
        {
            // When the IDispatcher is a synchronization context
            // Render operations are not thread-safe, so they need to be serialized.
            // Plus, any other logic that mutates state accessed during rendering also
            // needs not to run concurrently with rendering so should be dispatched to
            // the renderer's sync context.
            if (_dispatcher is SynchronizationContext synchronizationContext && SynchronizationContext.Current != synchronizationContext)
            {
                throw new InvalidOperationException(
                    "The current thread is not associated with the renderer's synchronization context. " +
                    "Use Invoke() or InvokeAsync() to switch execution to the renderer's synchronization " +
                    "context when triggering rendering or modifying any state accessed during rendering.");
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

                // Fire off the execution of OnAfterRenderAsync, but don't wait for it
                // if there is async work to be done.
                _ = InvokeRenderCompletedCalls(batch.UpdatedComponents);
            }
            finally
            {
                RemoveEventHandlerIds(_batchBuilder.DisposedEventHandlerIds.ToRange(), updateDisplayTask);
                _batchBuilder.Clear();
                _isBatchInProgress = false;
            }
        }

        private Task InvokeRenderCompletedCalls(ArrayRange<RenderTreeDiff> updatedComponents)
        {
            List<Task> batch = null;
            var array = updatedComponents.Array;
            for (var i = 0; i < updatedComponents.Count; i++)
            {
                var componentState = GetOptionalComponentState(array[i].ComponentId);
                if (componentState != null)
                {
                    // The component might be rendered and disposed in the same batch (if its parent
                    // was rendered later in the batch, and removed the child from the tree).
                    var task = componentState.NotifyRenderCompletedAsync();

                    // We want to avoid allocations per rendering. Avoid allocating a state machine or an accumulator
                    // unless we absolutely have to.
                    if (task.IsCompleted)
                    {
                        if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled)
                        {
                            // Nothing to do here.
                            continue;
                        }
                        else if (task.Status == TaskStatus.Faulted)
                        {
                            HandleException(task.Exception);
                            continue;
                        }
                    }

                    // The Task is incomplete.
                    // Queue up the task and we can inspect it later.
                    batch = batch ?? new List<Task>();
                    batch.Add(GetErrorHandledTask(task));
                }
            }

            return batch != null ?
                Task.WhenAll(batch) :
                Task.CompletedTask;
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

        private async Task GetErrorHandledTask(Task taskToHandle)
        {
            try
            {
                await taskToHandle;
            }
            catch (Exception ex)
            {
                if (!taskToHandle.IsCanceled)
                {
                    // Ignore errors due to task cancellations.
                    HandleException(ex);
                }
            }
        }

        /// <summary>
        /// Releases all resources currently used by this <see cref="Renderer"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this method is being invoked by <see cref="IDisposable.Dispose"/>, otherwise <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
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
                        HandleException(exception);
                    }
                }
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
