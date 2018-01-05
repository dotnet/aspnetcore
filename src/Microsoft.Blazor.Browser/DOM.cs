// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Browser.Interop;
using Microsoft.Blazor.Components;
using Microsoft.Blazor.UITree;

namespace Microsoft.Blazor.Browser
{
    /// <summary>
    /// Provides mechanisms for displaying Blazor components in a browser Document
    /// Object Model (DOM).
    /// </summary>
    public static class DOM
    {
        /// <summary>
        /// Associates the specified component with the specified DOM element, causing the
        /// component to be displayed there.
        /// </summary>
        /// <param name="elementSelector">A CSS selector that identifies a unique DOM element.</param>
        /// <param name="component">The component to be displayed in the DOM element.</param>
        public static void AttachComponent(string elementSelector, IComponent component)
        {
            var renderState = DOMComponentRenderState.GetOrCreate(component);

            RegisteredFunction.InvokeUnmarshalled<string, string, object>(
                "_blazorAttachComponentToElement", elementSelector, renderState.DOMComponentId);

            RefreshComponentInDOM(renderState);
        }

        private static void RefreshComponentInDOM(DOMComponentRenderState renderState)
        {
            var tree = renderState.UpdateRender();
            RegisteredFunction.InvokeUnmarshalled<string, UITreeNode[], int, object>(
                "_blazorRender",
                renderState.DOMComponentId,
                tree.Array,
                tree.Count);
        }
    }
}
