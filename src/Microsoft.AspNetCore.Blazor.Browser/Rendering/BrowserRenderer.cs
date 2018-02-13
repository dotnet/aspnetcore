// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Interop;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering <see cref="IComponent"/> instances in a
    /// web browser, dispatching events to them, and refreshing the UI as required.
    /// </summary>
    public class BrowserRenderer : Renderer, IDisposable
    {
        private readonly int _browserRendererId;

        /// <summary>
        /// Constructs an instance of <see cref="BrowserRenderer"/>.
        /// </summary>
        public BrowserRenderer()
        {
            _browserRendererId = BrowserRendererRegistry.Add(this);
        }

        internal void DispatchBrowserEvent(int componentId, int eventHandlerId, UIEventArgs eventArgs)
            => DispatchEvent(componentId, eventHandlerId, eventArgs);

        /// <summary>
        /// Attaches a new root component to the renderer,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <typeparam name="TComponent">The type of the component.</typeparam>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent<TComponent>(string domElementSelector)
            where TComponent: IComponent
        {
            AddComponent(typeof(TComponent), domElementSelector);
        }

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="BrowserRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        public void AddComponent(Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignComponentId(component);
            RegisteredFunction.InvokeUnmarshalled<int, string, int, object>(
                "attachComponentToElement",
                _browserRendererId,
                domElementSelector,
                componentId);
            component.SetParameters(ParameterCollection.Empty);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            BrowserRendererRegistry.TryRemove(_browserRendererId);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(RenderBatch batch)
        {
            RegisteredFunction.InvokeUnmarshalled<int, RenderBatch, object>(
                "renderBatch",
                _browserRendererId,
                batch);
        }
    }
}
