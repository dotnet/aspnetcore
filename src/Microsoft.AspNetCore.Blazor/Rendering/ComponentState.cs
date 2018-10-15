// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    /// <summary>
    /// Tracks the rendering state associated with an <see cref="IComponent"/> instance
    /// within the context of a <see cref="Renderer"/>. This is an internal implementation
    /// detail of <see cref="Renderer"/>.
    /// </summary>
    internal class ComponentState
    {
        private readonly int _componentId; // TODO: Change the type to 'long' when the Mono runtime has more complete support for passing longs in .NET->JS calls
        private readonly ComponentState _parentComponentState;
        private readonly IComponent _component;
        private readonly Renderer _renderer;
        private readonly IReadOnlyList<CascadingParameterState> _cascadingParameters;
        private RenderTreeBuilder _renderTreeBuilderCurrent;
        private RenderTreeBuilder _renderTreeBuilderPrevious;
        private ArrayBuilder<RenderTreeFrame> _latestDirectParametersSnapshot; // Lazily instantiated
        private bool _componentWasDisposed;

        public int ComponentId => _componentId;
        public IComponent Component => _component;
        public ComponentState ParentComponentState => _parentComponentState;

        /// <summary>
        /// Constructs an instance of <see cref="ComponentState"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> with which the new instance should be associated.</param>
        /// <param name="componentId">The externally visible identifier for the <see cref="IComponent"/>. The identifier must be unique in the context of the <see cref="Renderer"/>.</param>
        /// <param name="component">The <see cref="IComponent"/> whose state is being tracked.</param>
        /// <param name="parentComponentState">The <see cref="ComponentState"/> for the parent component, or null if this is a root component.</param>
        public ComponentState(Renderer renderer, int componentId, IComponent component, ComponentState parentComponentState)
        {
            _componentId = componentId;
            _parentComponentState = parentComponentState;
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _cascadingParameters = CascadingParameterState.FindCascadingParameters(this);
            _renderTreeBuilderCurrent = new RenderTreeBuilder(renderer);
            _renderTreeBuilderPrevious = new RenderTreeBuilder(renderer);

            if (_cascadingParameters != null)
            {
                AddCascadingParameterSubscriptions();
            }
        }

        public void RenderIntoBatch(RenderBatchBuilder batchBuilder, RenderFragment renderFragment)
        {
            // A component might be in the render queue already before getting disposed by an
            // earlier entry in the render queue. In that case, rendering is a no-op.
            if (_componentWasDisposed)
            {
                return;
            }

            // Swap the old and new tree builders
            (_renderTreeBuilderCurrent, _renderTreeBuilderPrevious) = (_renderTreeBuilderPrevious, _renderTreeBuilderCurrent);

            _renderTreeBuilderCurrent.Clear();
            renderFragment(_renderTreeBuilderCurrent);

            var diff = RenderTreeDiffBuilder.ComputeDiff(
                _renderer,
                batchBuilder,
                _componentId,
                _renderTreeBuilderPrevious.GetFrames(),
                _renderTreeBuilderCurrent.GetFrames());
            batchBuilder.UpdatedComponentDiffs.Append(diff);
        }

        public void DisposeInBatch(RenderBatchBuilder batchBuilder)
        {
            _componentWasDisposed = true;
 
            // TODO: Handle components throwing during dispose. Shouldn't break the whole render batch.
            if (_component is IDisposable disposable)
            {
                disposable.Dispose();
            }

            RenderTreeDiffBuilder.DisposeFrames(batchBuilder, _renderTreeBuilderCurrent.GetFrames());

            if (_cascadingParameters != null)
            {
                RemoveCascadingParameterSubscriptions();
            }
        }

        public void DispatchEvent(EventHandlerInvoker binding, UIEventArgs eventArgs)
        {
            if (_component is IHandleEvent handleEventComponent)
            {
                handleEventComponent.HandleEvent(binding, eventArgs);
            }
            else
            {
                throw new InvalidOperationException(
                    $"The component of type {_component.GetType().FullName} cannot receive " +
                    $"events because it does not implement {typeof(IHandleEvent).FullName}.");
            }
        }

        public void NotifyRenderCompleted()
            => (_component as IHandleAfterRender)?.OnAfterRender();

        public void SetDirectParameters(ParameterCollection parameters)
        {
            // Note: We should be careful to ensure that the framework never calls
            // IComponent.SetParameters directly elsewhere. We should only call it
            // via ComponentState.SetParameters (or NotifyCascadingValueChanged below).
            // If we bypass this, the component won't receive the cascading parameters nor
            // will it update its snapshot of direct parameters.

            // TODO: Consider adding a "static" mode for tree params in which we don't
            // subscribe for updates, and hence don't have to do any of the parameter
            // snapshotting. This would be useful for things like FormContext that aren't
            // going to change.

            if (_cascadingParameters != null)
            {
                // We may need to replay these direct parameters later (in NotifyCascadingValueChanged),
                // but we can't guarantee that the original underlying data won't have mutated in the
                // meantime, since it's just an index into the parent's RenderTreeFrames buffer.
                if (_latestDirectParametersSnapshot == null)
                {
                    _latestDirectParametersSnapshot = new ArrayBuilder<RenderTreeFrame>();
                }
                parameters.CaptureSnapshot(_latestDirectParametersSnapshot);

                parameters = parameters.WithCascadingParameters(_cascadingParameters);
            }

            Component.SetParameters(parameters);
        }

        public void NotifyCascadingValueChanged()
        {
            var directParams = _latestDirectParametersSnapshot != null
                ? new ParameterCollection(_latestDirectParametersSnapshot.Buffer, 0)
                : ParameterCollection.Empty;
            var allParams = directParams.WithCascadingParameters(_cascadingParameters);
            Component.SetParameters(allParams);
        }

        private void AddCascadingParameterSubscriptions()
        {
            var numCascadingParameters = _cascadingParameters.Count;
            for (var i = 0; i < numCascadingParameters; i++)
            {
                _cascadingParameters[i].ValueSupplier.Subscribe(this);
            }
        }

        private void RemoveCascadingParameterSubscriptions()
        {
            var numCascadingParameters = _cascadingParameters.Count;
            for (var i = 0; i < numCascadingParameters; i++)
            {
                _cascadingParameters[i].ValueSupplier.Unsubscribe(this);
            }
        }
    }
}
