// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Tracks the rendering state associated with an <see cref="IComponent"/> instance
    /// within the context of a <see cref="Renderer"/>. This is an internal implementation
    /// detail of <see cref="Renderer"/>.
    /// </summary>
    internal class ComponentState : IDisposable
    {
        private readonly Renderer _renderer;
        private readonly IReadOnlyList<CascadingParameterState> _cascadingParameters;
        private readonly bool _hasAnyCascadingParameterSubscriptions;
        private RenderTreeBuilder _renderTreeBuilderPrevious;
        private ArrayBuilder<RenderTreeFrame> _latestDirectParametersSnapshot; // Lazily instantiated
        private bool _componentWasDisposed;

        /// <summary>
        /// Constructs an instance of <see cref="ComponentState"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> with which the new instance should be associated.</param>
        /// <param name="componentId">The externally visible identifier for the <see cref="IComponent"/>. The identifier must be unique in the context of the <see cref="Renderer"/>.</param>
        /// <param name="component">The <see cref="IComponent"/> whose state is being tracked.</param>
        /// <param name="parentComponentState">The <see cref="ComponentState"/> for the parent component, or null if this is a root component.</param>
        public ComponentState(Renderer renderer, int componentId, IComponent component, ComponentState parentComponentState)
        {
            ComponentId = componentId;
            ParentComponentState = parentComponentState;
            Component = component ?? throw new ArgumentNullException(nameof(component));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _cascadingParameters = CascadingParameterState.FindCascadingParameters(this);
            CurrentRenderTree = new RenderTreeBuilder();
            _renderTreeBuilderPrevious = new RenderTreeBuilder();

            if (_cascadingParameters != null)
            {
                _hasAnyCascadingParameterSubscriptions = AddCascadingParameterSubscriptions();
            }
        }

        // TODO: Change the type to 'long' when the Mono runtime has more complete support for passing longs in .NET->JS calls
        public int ComponentId { get; }
        public IComponent Component { get; }
        public ComponentState ParentComponentState { get; }
        public RenderTreeBuilder CurrentRenderTree { get; private set; }

        public void RenderIntoBatch(RenderBatchBuilder batchBuilder, RenderFragment renderFragment)
        {
            // A component might be in the render queue already before getting disposed by an
            // earlier entry in the render queue. In that case, rendering is a no-op.
            if (_componentWasDisposed)
            {
                return;
            }

            // Swap the old and new tree builders
            (CurrentRenderTree, _renderTreeBuilderPrevious) = (_renderTreeBuilderPrevious, CurrentRenderTree);

            CurrentRenderTree.Clear();
            renderFragment(CurrentRenderTree);

            var diff = RenderTreeDiffBuilder.ComputeDiff(
                _renderer,
                batchBuilder,
                ComponentId,
                _renderTreeBuilderPrevious.GetFrames(),
                CurrentRenderTree.GetFrames());
            batchBuilder.UpdatedComponentDiffs.Append(diff);
            batchBuilder.InvalidateParameterViews();
        }

        public bool TryDisposeInBatch(RenderBatchBuilder batchBuilder, out Exception exception)
        {
            _componentWasDisposed = true;
            exception = null;

            try
            {
                if (Component is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // We don't expect these things to throw.
            RenderTreeDiffBuilder.DisposeFrames(batchBuilder, CurrentRenderTree.GetFrames());

            if (_hasAnyCascadingParameterSubscriptions)
            {
                RemoveCascadingParameterSubscriptions();
            }

            DisposeBuffers();

            return exception == null;
        }

        // Callers expect this method to always return a faulted task.
        public Task NotifyRenderCompletedAsync()
        {
            if (Component is IHandleAfterRender handlerAfterRender)
            {
                try
                {
                    return handlerAfterRender.OnAfterRenderAsync();
                }
                catch (OperationCanceledException cex)
                {
                    return Task.FromCanceled(cex.CancellationToken);
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }

            return Task.CompletedTask;
        }

        public void SetDirectParameters(ParameterView parameters)
        {
            // Note: We should be careful to ensure that the framework never calls
            // IComponent.SetParametersAsync directly elsewhere. We should only call it
            // via ComponentState.SetDirectParameters (or NotifyCascadingValueChanged below).
            // If we bypass this, the component won't receive the cascading parameters nor
            // will it update its snapshot of direct parameters.

            if (_hasAnyCascadingParameterSubscriptions)
            {
                // We may need to replay these direct parameters later (in NotifyCascadingValueChanged),
                // but we can't guarantee that the original underlying data won't have mutated in the
                // meantime, since it's just an index into the parent's RenderTreeFrames buffer.
                if (_latestDirectParametersSnapshot == null)
                {
                    _latestDirectParametersSnapshot = new ArrayBuilder<RenderTreeFrame>();
                }

                parameters.CaptureSnapshot(_latestDirectParametersSnapshot);
            }

            if (_cascadingParameters != null)
            {
                parameters = parameters.WithCascadingParameters(_cascadingParameters);
            }

            _renderer.AddToPendingTasks(Component.SetParametersAsync(parameters));
        }

        public void NotifyCascadingValueChanged(in ParameterViewLifetime lifetime)
        {
            var directParams = _latestDirectParametersSnapshot != null
                ? new ParameterView(lifetime, _latestDirectParametersSnapshot.Buffer, 0)
                : ParameterView.Empty;
            var allParams = directParams.WithCascadingParameters(_cascadingParameters);
            var task = Component.SetParametersAsync(allParams);
            _renderer.AddToPendingTasks(task);
        }

        private bool AddCascadingParameterSubscriptions()
        {
            var hasSubscription = false;
            var numCascadingParameters = _cascadingParameters.Count;

            for (var i = 0; i < numCascadingParameters; i++)
            {
                var valueSupplier = _cascadingParameters[i].ValueSupplier;
                if (!valueSupplier.CurrentValueIsFixed)
                {
                    valueSupplier.Subscribe(this);
                    hasSubscription = true;
                }
            }

            return hasSubscription;
        }

        private void RemoveCascadingParameterSubscriptions()
        {
            var numCascadingParameters = _cascadingParameters.Count;
            for (var i = 0; i < numCascadingParameters; i++)
            {
                var supplier = _cascadingParameters[i].ValueSupplier;
                if (!supplier.CurrentValueIsFixed)
                {
                    supplier.Unsubscribe(this);
                }
            }
        }

        public void Dispose()
        {
            DisposeBuffers();

            if (Component is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private void DisposeBuffers()
        {
            ((IDisposable)_renderTreeBuilderPrevious).Dispose();
            ((IDisposable)CurrentRenderTree).Dispose();
            _latestDirectParametersSnapshot?.Dispose();
        }
    }
}
