// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.JSInterop;
using Mono.WebAssembly.Interop;
using System;

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
        public BrowserRenderer(): this(new BrowserServiceProvider())
        {
        }

        /// <summary>
        /// Constructs an instance of <see cref="BrowserRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use when initializing components.</param>
        public BrowserRenderer(IServiceProvider serviceProvider): base(serviceProvider)
        {
            // The browser renderer registers and unregisters itself with the static
            // registry. This works well with the WebAssembly runtime, and is simple for the
            // case where Blazor is running in process.
            _browserRendererId = RendererRegistry.Current.Add(this);
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
            var componentId = AssignRootComponentId(component);

            // The only reason we're calling this synchronously is so that, if it throws,
            // we get the exception back *before* attempting the first UpdateDisplay
            // (otherwise the logged exception will come from UpdateDisplay instead of here)
            // When implementing support for out-of-process runtimes, we'll need to call this
            // asynchronously and ensure we surface any exceptions correctly.
            ((IJSInProcessRuntime)JSRuntime.Current).Invoke<object>(
                "Blazor._internal.attachRootComponentToElement",
                _browserRendererId,
                domElementSelector,
                componentId);

            RenderRootComponent(componentId);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            RendererRegistry.Current.TryRemove(_browserRendererId);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(in RenderBatch batch)
        {
            if (JSRuntime.Current is MonoWebAssemblyJSRuntime mono)
            {
                mono.InvokeUnmarshalled<int, RenderBatch, object>(
                    "Blazor._internal.renderBatch",
                    _browserRendererId,
                    batch);
            }
            else
            {
                // When implementing support for an out-of-process JS runtime, we'll need to
                // do something here to serialize and transmit the RenderBatch efficiently.
                throw new NotImplementedException("TODO: Support BrowserRenderer.UpdateDisplay on other runtimes.");
            }
        }
    }
}
