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

        internal void RenderNewBatchInternal(int componentId)
            => RenderNewBatch(componentId);

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
                "attachComponentToElement",
                _browserRendererId,
                domElementSelector,
                componentId);
            _rootComponents.Add(component);

            RenderNewBatch(componentId);
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
            RenderTreeDiff renderTreeDiff)
        {
            RegisteredFunction.InvokeUnmarshalled<RenderComponentArgs, object>(
                "renderRenderTree",
                new RenderComponentArgs
                {
                    BrowserRendererId = _browserRendererId,
                    ComponentId = componentId,
                    RenderTreeEdits = renderTreeDiff.Edits.Array,
                    RenderTreeEditsLength = renderTreeDiff.Edits.Count,
                    RenderTree = renderTreeDiff.CurrentState.Array
                });
        }

        // Encapsulates the data we pass to the JS rendering function
        private struct RenderComponentArgs
        {
            // Important: If you edit this struct, keep it in sync with RenderComponentArgs.ts

            public int BrowserRendererId;
            public int ComponentId;
            public RenderTreeEdit[] RenderTreeEdits;
            public int RenderTreeEditsLength;
            public RenderTreeNode[] RenderTree;
        }
    }
}
