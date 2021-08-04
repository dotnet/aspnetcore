// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A <see cref="Renderer"/> that attaches its components to a browser DOM.
    /// </summary>
    public abstract class WebRenderer : Renderer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DotNetObjectReference<WebRendererInteropMethods> _interopMethodsReference;

        /// <summary>
        /// Constructs an instance of <see cref="WebRenderer"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to be used when initializing components.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="jsonOptions">The <see cref="JsonSerializerOptions"/>.</param>
        public WebRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, JsonSerializerOptions jsonOptions)
            : base(serviceProvider, loggerFactory)
        {
            _serviceProvider = serviceProvider;
            _interopMethodsReference = DotNetObjectReference.Create(
                new WebRendererInteropMethods(this, jsonOptions));
        }

        /// <summary>
        /// Instantiates a root component and attaches it to the browser within the specified element.
        /// </summary>
        /// <param name="componentType">The type of the component.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        /// <returns>The new component ID.</returns>
        protected internal int AddRootComponent([DynamicallyAccessedMembers(Component)] Type componentType, string domElementSelector)
        {
            var component = InstantiateComponent(componentType);
            var componentId = AssignRootComponentId(component);
            AttachRootComponentToBrowser(componentId, domElementSelector);
            return componentId;
        }

        /// <summary>
        /// Called by the framework to give a location for the specified root component in the browser DOM.
        /// </summary>
        /// <param name="componentId">The component ID.</param>
        /// <param name="domElementSelector">A CSS selector that uniquely identifies a DOM element.</param>
        protected abstract void AttachRootComponentToBrowser(int componentId, string domElementSelector);

        /// <summary>
        /// Enables support for adding, updating, and removing root components from JavaScript.
        /// </summary>
        /// <param name="rendererId">An identifier for the renderer that will be used by JavaScript code.</param>
        /// <param name="interop">The object that provides JS-callable methods for adding, updating, and removing root components.</param>
        /// <returns>A task representing the completion of the operation.</returns>
        protected async ValueTask InitializeJSComponentSupportAsync(int rendererId, JSComponentInterop interop)
        {
            var jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();
            await interop.InitializeAsync(jsRuntime, this);
            await jsRuntime.InvokeVoidAsync("Blazor._internal.attachWebRendererInterop", rendererId, _interopMethodsReference);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _interopMethodsReference.Dispose();
            }

            base.Dispose(disposing);
        }

        private class WebRendererInteropMethods
        {
            private readonly Renderer _renderer;
            private readonly JsonSerializerOptions _jsonOptions;

            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(WebRendererInteropMethods))]
            public WebRendererInteropMethods(Renderer renderer, JsonSerializerOptions jsonOptions)
            {
                _renderer = renderer;
                _jsonOptions = jsonOptions;
            }

            [JSInvokable]
            public Task DispatchEventAsync(JsonElement eventDescriptor, JsonElement eventArgs)
            {
                var webEventData = WebEventData.Parse(_renderer, _jsonOptions, eventDescriptor, eventArgs);
                return _renderer.DispatchEventAsync(
                    webEventData.EventHandlerId,
                    webEventData.EventFieldInfo,
                    webEventData.EventArgs);
            }
        }
    }
}
