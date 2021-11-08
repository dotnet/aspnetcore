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

namespace Microsoft.AspNetCore.Components.RenderTree;

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
    /// <param name="jsComponentInterop">The <see cref="JSComponentInterop"/>.</param>
    public WebRenderer(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        JsonSerializerOptions jsonOptions,
        JSComponentInterop jsComponentInterop)
        : base(serviceProvider, loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _interopMethodsReference = DotNetObjectReference.Create(
            new WebRendererInteropMethods(this, jsonOptions, jsComponentInterop));

        // Supply a DotNetObjectReference to JS that it can use to call us back for events etc.
        jsComponentInterop.AttachToRenderer(this);
        var jsRuntime = _serviceProvider.GetRequiredService<IJSRuntime>();
        jsRuntime.InvokeVoidAsync(
            "Blazor._internal.attachWebRendererInterop",
            RendererId,
            _interopMethodsReference,
            jsComponentInterop.Configuration.JSComponentParametersByIdentifier,
            jsComponentInterop.Configuration.JSComponentIdentifiersByInitializer).Preserve();
    }

    /// <summary>
    /// Gets the identifier for the renderer.
    /// </summary>
    protected int RendererId { get; init; } // Only used on WebAssembly. Will be zero in other cases.

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

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _interopMethodsReference.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// A collection of JS invokable methods that the JS-side code can use when it needs to
    /// make calls in the context of a particular renderer. This object is never exposed to
    /// .NET code so is only reachable via JS.
    /// </summary>
    internal sealed class WebRendererInteropMethods
    {
        private readonly WebRenderer _renderer;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly JSComponentInterop _jsComponentInterop;

        [DynamicDependency(nameof(DispatchEventAsync))]
        public WebRendererInteropMethods(WebRenderer renderer, JsonSerializerOptions jsonOptions, JSComponentInterop jsComponentInterop)
        {
            _renderer = renderer;
            _jsonOptions = jsonOptions;
            _jsComponentInterop = jsComponentInterop;
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

        [JSInvokable] // Linker preserves this if you call RootComponents.Add
        public int AddRootComponent(string identifier, string domElementSelector)
            => _jsComponentInterop.AddRootComponent(identifier, domElementSelector);

        [JSInvokable] // Linker preserves this if you call RootComponents.Add
        public void SetRootComponentParameters(int componentId, int parameterCount, JsonElement parametersJson)
            => _jsComponentInterop.SetRootComponentParameters(componentId, parameterCount, parametersJson, _jsonOptions);

        [JSInvokable] // Linker preserves this if you call RootComponents.Add
        public void RemoveRootComponent(int componentId)
            => _jsComponentInterop.RemoveRootComponent(componentId);
    }
}
