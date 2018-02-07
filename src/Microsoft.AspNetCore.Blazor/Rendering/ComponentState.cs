// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;

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
        private readonly IComponent _component;
        private readonly Renderer _renderer;
        private readonly RenderTreeDiffComputer _diffComputer;
        private RenderTreeBuilder _renderTreeBuilderCurrent;
        private RenderTreeBuilder _renderTreeBuilderPrevious;

        /// <summary>
        /// Constructs an instance of <see cref="ComponentState"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="Renderer"/> with which the new instance should be associated.</param>
        /// <param name="componentId">The externally visible identifer for the <see cref="IComponent"/>. The identifier must be unique in the context of the <see cref="Renderer"/>.</param>
        /// <param name="component">The <see cref="IComponent"/> whose state is being tracked.</param>
        public ComponentState(Renderer renderer, int componentId, IComponent component)
        {
            _componentId = componentId;
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            _diffComputer = new RenderTreeDiffComputer(renderer);
            _renderTreeBuilderCurrent = new RenderTreeBuilder(renderer);
            _renderTreeBuilderPrevious = new RenderTreeBuilder(renderer);
        }

        /// <summary>
        /// Regenerates the <see cref="RenderTree"/> and adds the changes to the
        /// <paramref name="batchBuilder"/>.
        /// </summary>
        public void Render(RenderBatchBuilder batchBuilder)
        {
            // Swap the old and new tree builders
            (_renderTreeBuilderCurrent, _renderTreeBuilderPrevious) = (_renderTreeBuilderPrevious, _renderTreeBuilderCurrent);

            _renderTreeBuilderCurrent.Clear();
            _component.BuildRenderTree(_renderTreeBuilderCurrent);
            _diffComputer.ApplyNewRenderTreeVersion(
                batchBuilder,
                _componentId,
                _renderTreeBuilderPrevious.GetFrames(),
                _renderTreeBuilderCurrent.GetFrames());
        }

        /// <summary>
        /// Invokes the handler corresponding to an event.
        /// </summary>
        /// <param name="referenceTreeIndex">The index of the frame in the latest diff reference tree that holds the event handler to be invoked.</param>
        /// <param name="eventArgs">Arguments to be passed to the event handler.</param>
        public void DispatchEvent(int referenceTreeIndex, UIEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs));
            }

            var eventHandler = _diffComputer.TemporaryGetEventHandlerMethod(referenceTreeIndex);
            eventHandler.Invoke(eventArgs);

            // After any event, we synchronously re-render. Most of the time this means that
            // developers don't need to call Render() on their components explicitly.
            _renderer.RenderNewBatch(_componentId);
        }

        /// <summary>
        /// Notifies the component that it is being disposed.
        /// </summary>
        public void NotifyDisposed()
        {
            // TODO: Handle components throwing during dispose. Shouldn't break the whole render batch.
            if (_component is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
