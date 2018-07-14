// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Blazor.Browser.Rendering
{
    internal class RemoteRenderer : Renderer
    {
        private readonly int _id;
        private readonly IClientProxy _client;
        private readonly IJSRuntime _jsRuntime;
        private readonly RendererRegistry _rendererRegistry;

        /// <summary>
        /// Notifies when a rendering exception occured.
        /// </summary>
        public event EventHandler<Exception> UnhandledException;

        /// <summary>
        /// Creates a new <see cref="RemoteRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="rendererRegistry">The <see cref="RendererRegistry"/>.</param>
        /// <param name="jsRuntime">The <see cref="IJSRuntime"/>.</param>
        /// <param name="client">The <see cref="IClientProxy"/>.</param>
        public RemoteRenderer(
            IServiceProvider serviceProvider,
            RendererRegistry rendererRegistry,
            IJSRuntime jsRuntime,
            IClientProxy client)
            : base(serviceProvider)
        {
            _rendererRegistry = rendererRegistry;
            _jsRuntime = jsRuntime;
            _client = client;

            _id = _rendererRegistry.Add(this);
        }

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

            var attachComponentTask = _jsRuntime.InvokeAsync<object>(
                "Blazor._internal.attachRootComponentToElement",
                _id,
                domElementSelector,
                componentId);
            CaptureAsyncExceptions(attachComponentTask);

            component.SetParameters(ParameterCollection.Empty);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            _rendererRegistry.TryRemove(_id);
        }

        /// <inheritdoc />
        protected override void UpdateDisplay(in RenderBatch batch)
        {
            var task = _client.SendAsync("JS.RenderBatch", _id, batch);
            CaptureAsyncExceptions(task);
        }

        private void CaptureAsyncExceptions(Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    UnhandledException?.Invoke(this, t.Exception);
                }
            });
        }
    }
}
