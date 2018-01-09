// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.Rendering;
using Microsoft.Blazor.RenderTree;
using System;
using System.Collections.Generic;

namespace Microsoft.Blazor.Browser.Rendering
{
    /// <summary>
    /// Provides mechanisms for rendering <see cref="IComponent"/> instances in a
    /// web browser, dispatching events to them, and refreshing the UI as required.
    /// </summary>
    public class BrowserRenderer : Renderer, IDisposable
    {
        private readonly int _browserRendererId;

        // Ensures the explicitly-added components aren't GCed, because the browser
        // will still send events referencing them by ID. We only need to store the
        // top-level components, because the associated ComponentState will reference
        // all the reachable descendant components of each.
        private IList<IComponent> _rootComponents = new List<IComponent>();

        /// <summary>
        /// Constructs an instance of <see cref="BrowserRenderer"/>.
        /// </summary>
        public BrowserRenderer()
        {
            _browserRendererId = BrowserRendererRegistry.Add(this);
        }

        internal void DispatchBrowserEvent(int componentId, int renderTreeIndex, UIEventArgs eventArgs)
            => DispatchEvent(componentId, renderTreeIndex, eventArgs);

        internal void RenderComponentInternal(int componentId)
            => RenderComponent(componentId);

        /// <summary>
        /// Associates the <see cref="IComponent"/> with the <see cref="BrowserRenderer"/>,
        /// causing it to be displayed in the specified DOM element.
        /// </summary>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <param name="component">The <see cref="IComponent"/>.</param>
        public void AddComponent(string domElementSelector, IComponent component)
        {
            var componentId = AssignComponentId(component);
            RegisteredFunction.InvokeUnmarshalled<int, string, int, object>(
                "_blazorAttachComponentToElement",
                _browserRendererId,
                domElementSelector,
                componentId);
            _rootComponents.Add(component);

            RenderComponent(componentId);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            BrowserRendererRegistry.TryRemove(_browserRendererId);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(
            int componentId,
            ArraySegment<RenderTreeNode> renderTree)
        {
            RegisteredFunction.InvokeUnmarshalled<RenderComponentArgs, object>(
                "_blazorRender",
                new RenderComponentArgs
                {
                    BrowserRendererId = _browserRendererId,
                    ComponentId = componentId,
                    RenderTree = renderTree.Array,
                    RenderTreeLength = renderTree.Count
                });
        }

        // Encapsulates the data we pass to the JS rendering function
        private struct RenderComponentArgs
        {
            public int BrowserRendererId;
            public int ComponentId;
            public RenderTreeNode[] RenderTree;
            public int RenderTreeLength;
        }
    }
}
