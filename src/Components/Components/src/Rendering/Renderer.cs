// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering hierarchies of <see cref="IComponent"/> instances,
    /// dispatching events to them, and notifying when the user interface is being updated.
    /// </summary>
    public abstract partial class Renderer : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, ComponentState> _componentStateById = new Dictionary<int, ComponentState>();
        private readonly RenderBatchBuilder _batchBuilder = new RenderBatchBuilder();
        private readonly Dictionary<ulong, EventCallback> _eventBindings = new Dictionary<ulong, EventCallback>();
        private readonly Dictionary<ulong, ulong> _eventHandlerIdReplacements = new Dictionary<ulong, ulong>();
        private readonly ILogger<Renderer> _logger;

        private int _nextComponentId = 0; // TODO: change to 'long' when Mono .NET->JS interop supports it
        private bool _isBatchInProgress;
        private ulong _lastEventHandlerId;
        private List<Task> _pendingTasks;

        /// <summary>
        /// Allows the caller to handle exceptions from the SynchronizationContext when one is available.
        /// </summary>
        public event UnhandledExceptionEventHandler UnhandledSynchronizationException
        {
            add
            {
                Dispatcher.UnhandledException += value;
            }
            remove
            {
                Dispatcher.UnhandledException -= value;
            }
        }

        /// <summary>
        /// Constructs an instance of <see cref="Renderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initializing components.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public Renderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _serviceProvider = serviceProvider;
            _logger = loggerFactory.CreateLogger<Renderer>();
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Components.Dispatcher" /> associated with this <see cref="Renderer" />.
        /// </summary>
        public abstract Dispatcher Dispatcher { get; }

        /// <summary>
        /// Constructs a new component of the specified type.
        /// </summary>
        /// <param name="componentType">The type of the component to instantiate.</param>
        /// <returns>The component instance.</returns>
        protected IComponent InstantiateComponent(Type componentType)
            => ComponentFactory.Instance.InstantiateComponent(_serviceProvider, componentType);

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
        private protected ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId) => GetRequiredComponentState(componentId).CurrentRenderTree.GetFrames();

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
            return RenderRootComponentAsync(componentId, ParameterView.Empty);
        }

        /// <summary>
        /// Performs the first render for a root component, waiting for this component and all
        /// children components to finish rendering in case there is any asynchronous work being
        /// done by any of the components. After this, the root component
        /// makes its own decisions about when to re-render, so there is no need to call
        /// this more than once.
        /// </summary>
        /// <param name="componentId">The ID returned by <see cref="AssignRootComponentId(IComponent)"/>.</param>
        /// <param name="initialParameters">The <see cref="ParameterView"/>with the initial parameters to use for rendering.</param>
        /// <remarks>
        /// Rendering a root component is an asynchronous operation. Clients may choose to not await the returned task to
        /// start, but not wait for the entire render to complete.
        /// </remarks>
        protected async Task RenderRootComponentAsync(int componentId, ParameterView initialParameters)
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
            Log.InitializingComponent(_logger, componentState, parentComponentState);
            _componentStateById.Add(componentId, componentState);
            component.Attach(new RenderHandle(this, componentId));
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
        /// <param name="fieldInfo">Information that the renderer can use to update the state of the existing render tree to match the UI.</param>
        /// <returns>
        /// A <see cref="Task"/> which will complete once all asynchronous processing related to the event
        /// has completed.
        /// </returns>
        public virtual Task DispatchEventAsync(ulong eventHandlerId, EventFieldInfo fieldInfo, EventArgs eventArgs)
        {
            EnsureSynchronizationContext();

            if (!_eventBindings.TryGetValue(eventHandlerId, out var callback))
            {
                throw new ArgumentException($"There is no event handler with ID {eventHandlerId}");
            }

            Log.HandlingEvent(_logger, eventHandlerId, eventArgs);

            if (fieldInfo != null)
            {
                var latestEquivalentEventHandlerId = FindLatestEventHandlerIdInChain(eventHandlerId);
                UpdateRenderTreeToMatchClientState(latestEquivalentEventHandlerId, fieldInfo);
            }

            Task task = null;
            try
            {
                // The event handler might request multiple renders in sequence. Capture them
                // all in a single batch.
                _isBatchInProgress = true;

                task = callback.InvokeAsync(eventArgs);
            }
            catch (Exception e)
            {
                HandleException(e);
                return Task.CompletedTask;
            }
            finally
            {
                _isBatchInProgress = false;

                // Since the task has yielded - process any queued rendering work before we return control
                // to the caller.
                ProcessPendingRender();
            }

            // Task completed synchronously or is still running. We already processed all of the rendering
            // work that was queued so let our error handler deal with it.
            return GetErrorHandledTask(task);
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
        internal void AddToRenderQueue(int componentId, RenderFragment renderFragment)
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
                ProcessPendingRender();
            }
        }

        internal void TrackReplacedEventHandlerId(ulong oldEventHandlerId, ulong newEventHandlerId)
        {
            // Tracking the chain of old->new replacements allows us to interpret incoming EventFieldInfo
            // values even if they refer to an event handler ID that's since been superseded. This is essential
            // for tree patching to work in an async environment.
            _eventHandlerIdReplacements.Add(oldEventHandlerId, newEventHandlerId);
        }

        private ulong FindLatestEventHandlerIdInChain(ulong eventHandlerId)
        {
            while (_eventHandlerIdReplacements.TryGetValue(eventHandlerId, out var replacementEventHandlerId))
            {
                eventHandlerId = replacementEventHandlerId;
            }

            return eventHandlerId;
        }

        private void EnsureSynchronizationContext()
        {
            // Render operations are not thread-safe, so they need to be serialized by the dispatcher.
            // Plus, any other logic that mutates state accessed during rendering also
            // needs not to run concurrently with rendering so should be dispatched to
            // the renderer's sync context.
            if (!Dispatcher.CheckAccess())
            {
                throw new InvalidOperationException(
                    "The current thread is not associated with the Dispatcher. " +
                    "Use Invoke() or InvokeAsync() to switch execution to the Dispatcher when " +
                    "triggering rendering or modifying any state accessed during rendering.");
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

        /// <summary>
        /// Processses pending renders requests from components if there are any.
        /// </summary>
        protected virtual void ProcessPendingRender()
        {
            ProcessRenderQueue();
        }

        private void ProcessRenderQueue()
        {
            EnsureSynchronizationContext();

            if (_isBatchInProgress)
            {
                throw new InvalidOperationException("Cannot start a batch when one is already in progress.");
            }

            _isBatchInProgress = true;
            var updateDisplayTask = Task.CompletedTask;

            try
            {
                if (_batchBuilder.ComponentRenderQueue.Count == 0)
                {
                    return;
                }

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
                _ = InvokeRenderCompletedCalls(batch.UpdatedComponents, updateDisplayTask);
            }
            catch (Exception e)
            {
                // Ensure we catch errors while running the render functions of the components.
                HandleException(e);
                return;
            }
            finally
            {
                RemoveEventHandlerIds(_batchBuilder.DisposedEventHandlerIds.ToRange(), updateDisplayTask);
                _batchBuilder.ClearStateForCurrentBatch();
                _isBatchInProgress = false;
            }

            // An OnAfterRenderAsync callback might have queued more work synchronously.
            // Note: we do *not* re-render implicitly after the OnAfterRenderAsync-returned
            // task (that would be an infinite loop). We only render after an explicit render
            // request (e.g., StateHasChanged()).
            if (_batchBuilder.ComponentRenderQueue.Count > 0)
            {
                ProcessRenderQueue();
            }
        }

        private Task InvokeRenderCompletedCalls(ArrayRange<RenderTreeDiff> updatedComponents, Task updateDisplayTask)
        {
            if (updateDisplayTask.IsCanceled)
            {
                // The display update was canceled.
                // This can be due to a timeout on the components server-side case, or the renderer being disposed.

                // The latter case is normal during prerendering, as the render never fully completes (the display never
                // gets updated, no references get populated and JavaScript interop is not available) and we simply discard
                // the renderer after producing the prerendered content.
                return Task.CompletedTask;
            }
            if (updateDisplayTask.IsFaulted)
            {
                // The display update failed so we don't care any more about running on render completed
                // fallbacks as the entire rendering process is going to be torn down.
                HandleException(updateDisplayTask.Exception);
                return Task.CompletedTask;
            }

            if (!updateDisplayTask.IsCompleted)
            {
                var updatedComponentsId = new int[updatedComponents.Count];
                var updatedComponentsArray = updatedComponents.Array;
                for (int i = 0; i < updatedComponentsId.Length; i++)
                {
                    updatedComponentsId[i] = updatedComponentsArray[i].ComponentId;
                }

                return InvokeRenderCompletedCallsAfterUpdateDisplayTask(updateDisplayTask, updatedComponentsId);
            }

            List<Task> batch = null;
            var array = updatedComponents.Array;
            for (var i = 0; i < updatedComponents.Count; i++)
            {
                var componentState = GetOptionalComponentState(array[i].ComponentId);
                if (componentState != null)
                {
                    NotifyRenderCompleted(componentState, ref batch);
                }
            }

            return batch != null ?
                Task.WhenAll(batch) :
                Task.CompletedTask;

        }

        private async Task InvokeRenderCompletedCallsAfterUpdateDisplayTask(
            Task updateDisplayTask,
            int[] updatedComponents)
        {
            try
            {
                await updateDisplayTask;
            }
            catch // avoiding exception filters for AOT runtimes
            {
                if (updateDisplayTask.IsCanceled)
                {
                    return;
                }

                HandleException(updateDisplayTask.Exception);
                return;
            }

            List<Task> batch = null;
            var array = updatedComponents;
            for (var i = 0; i < updatedComponents.Length; i++)
            {
                var componentState = GetOptionalComponentState(array[i]);
                if (componentState != null)
                {
                    NotifyRenderCompleted(componentState, ref batch);
                }
            }

            var result = batch != null ?
                Task.WhenAll(batch) :
                Task.CompletedTask;

            await result;
        }

        private void NotifyRenderCompleted(ComponentState state, ref List<Task> batch)
        {
            // The component might be rendered and disposed in the same batch (if its parent
            // was rendered later in the batch, and removed the child from the tree).
            // This can also happen between batches if the UI takes some time to update and within
            // that time the component gets removed out of the tree because the parent chose not to
            // render it in a later batch.
            // In any of the two cases mentioned happens, OnAfterRenderAsync won't run but that is
            // ok.
            var task = state.NotifyRenderCompletedAsync();

            // We want to avoid allocations per rendering. Avoid allocating a state machine or an accumulator
            // unless we absolutely have to.
            if (task.IsCompleted)
            {
                if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled)
                {
                    // Nothing to do here.
                    return;
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    HandleException(task.Exception);
                    return;
                }
            }

            // The Task is incomplete.
            // Queue up the task and we can inspect it later.
            batch = batch ?? new List<Task>();
            batch.Add(GetErrorHandledTask(task));
        }

        private void RenderInExistingBatch(RenderQueueEntry renderQueueEntry)
        {
            var componentState = renderQueueEntry.ComponentState;
            Log.RenderingComponent(_logger, componentState);
            componentState.RenderIntoBatch(_batchBuilder, renderQueueEntry.RenderFragment);

            List<Exception> exceptions = null;

            // Process disposal queue now in case it causes further component renders to be enqueued
            while (_batchBuilder.ComponentDisposalQueue.Count > 0)
            {
                var disposeComponentId = _batchBuilder.ComponentDisposalQueue.Dequeue();
                var disposeComponentState = GetRequiredComponentState(disposeComponentId);
                Log.DisposingComponent(_logger, disposeComponentState);
                if (!disposeComponentState.TryDisposeInBatch(_batchBuilder, out var exception))
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
                _componentStateById.Remove(disposeComponentId);
                _batchBuilder.DisposedComponentIds.Append(disposeComponentId);
            }

            if (exceptions?.Count > 1)
            {
                HandleException(new AggregateException("Exceptions were encountered while disposing components.", exceptions));
            }
            else if (exceptions?.Count == 1)
            {
                HandleException(exceptions[0]);
            }
        }

        private void RemoveEventHandlerIds(ArrayRange<ulong> eventHandlerIds, Task afterTaskIgnoreErrors)
        {
            if (eventHandlerIds.Count == 0)
            {
                return;
            }

            if (afterTaskIgnoreErrors.IsCompleted)
            {
                var array = eventHandlerIds.Array;
                var count = eventHandlerIds.Count;
                for (var i = 0; i < count; i++)
                {
                    var eventHandlerIdToRemove = array[i];
                    _eventBindings.Remove(eventHandlerIdToRemove);
                    _eventHandlerIdReplacements.Remove(eventHandlerIdToRemove);
                }
            }
            else
            {
                _ = ContinueAfterTask(eventHandlerIds, afterTaskIgnoreErrors);
            }

            // Factor out the async part into a separate local method purely so, in the
            // synchronous case, there's no state machine or task construction
            async Task ContinueAfterTask(ArrayRange<ulong> eventHandlerIds, Task afterTaskIgnoreErrors)
            {
                // We need to delay the actual removal (e.g., until we've confirmed the client
                // has processed the batch and hence can be sure not to reuse the handler IDs
                // any further). We must clone the data because the underlying RenderBatchBuilder
                // may be reused and hence modified by an unrelated subsequent batch.
                var eventHandlerIdsClone = eventHandlerIds.Clone();

                try
                {
                    await afterTaskIgnoreErrors;
                }
                catch (Exception)
                {
                    // As per method contract, we're not error-handling the task.
                    // That remains the caller's business.
                }

                // We know the next execution will complete synchronously, so no infinite loop
                RemoveEventHandlerIds(eventHandlerIdsClone, Task.CompletedTask);
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

        private void UpdateRenderTreeToMatchClientState(ulong eventHandlerId, EventFieldInfo fieldInfo)
        {
            var componentState = GetOptionalComponentState(fieldInfo.ComponentId);
            if (componentState != null)
            {
                RenderTreeUpdater.UpdateToMatchClientState(
                    componentState.CurrentRenderTree,
                    eventHandlerId,
                    fieldInfo.FieldValue);
            }
        }

        /// <summary>
        /// Releases all resources currently used by this <see cref="Renderer"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this method is being invoked by <see cref="IDisposable.Dispose"/>, otherwise <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            // It's important that we handle all exceptions here before reporting any of them.
            // This way we can dispose all components before an error handler kicks in.
            List<Exception> exceptions = null;
            foreach (var componentState in _componentStateById.Values)
            {
                Log.DisposingComponent(_logger, componentState);

                if (componentState.Component is IDisposable disposable)
                {
                    try
                    {
                        componentState.Dispose();
                    }
                    catch (Exception exception)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(exception);
                    }
                }
            }

            _batchBuilder.Dispose();

            if (exceptions?.Count > 1)
            {
                HandleException(new AggregateException("Exceptions were encountered while disposing components.", exceptions));
            }
            else if (exceptions?.Count == 1)
            {
                HandleException(exceptions[0]);
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
