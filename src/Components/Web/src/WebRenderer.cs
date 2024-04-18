// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Web.Internal;
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
    private readonly DotNetObjectReference<WebRendererInteropMethods> _interopMethodsReference;
    private readonly int _rendererId;

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
        _interopMethodsReference = DotNetObjectReference.Create(
            new WebRendererInteropMethods(this, jsonOptions, jsComponentInterop));
        _rendererId = GetWebRendererId();

        // Supply a DotNetObjectReference to JS that it can use to call us back for events etc.
        jsComponentInterop.AttachToRenderer(this);
        var jsRuntime = serviceProvider.GetRequiredService<IJSRuntime>();
        AttachWebRendererInterop(jsRuntime, jsonOptions, jsComponentInterop);
    }

    /// <summary>
    /// Gets the identifier for the renderer.
    /// </summary>
    protected int RendererId
    {
        get => _rendererId;

        [Obsolete($"The renderer ID can be assigned by overriding '{nameof(GetWebRendererId)}'.")]
        init { /* No-op */ }
    }

    /// <summary>
    /// Allocates an identifier for the renderer.
    /// </summary>
    protected virtual int GetWebRendererId()
    {
        // We return '0' by default, which is reserved so that classes deriving from this
        // type don't need to worry about allocating an ID unless they're using multiple renderers.
        // As soon as multiple renderers are used, this needs to return a unique identifier.
        return 0;
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _interopMethodsReference.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AttachWebRendererInterop(IJSRuntime jsRuntime, JsonSerializerOptions jsonOptions, JSComponentInterop jsComponentInterop)
    {
        const string JSMethodIdentifier = "Blazor._internal.attachWebRendererInterop";

        if (jsRuntime is IInternalWebJSInProcessRuntime inProcessRuntime)
        {
            // Fast path for WebAssembly: Rather than using the JSRuntime to serialize
            // parameters, we utilize the source-generated WebRendererSerializerContext
            // for a faster JsonTypeInfo resolution.

            // We resolve a JsonTypeInfo for DotNetObjectReference<WebRendererInteropMethods> from
            // the JS runtime's JsonConverters. This is because adding DotNetObjectReference<T> as
            // a supported type in the JsonSerializerContext generates unnecessary code to produce
            // JsonTypeInfo for all the types referenced by both DotNetObjectReference<T> and its
            // generic type argument.
            var interopMethodsReferenceJsonTypeInfo = GetJsonTypeInfoFromJsonConverterFactories<DotNetObjectReference<WebRendererInteropMethods>>(
                jsonOptions.Converters,
                WebRendererSerializerContext.Default.Options);

            var rendererIdJson = _rendererId.ToString(CultureInfo.InvariantCulture);
            var interopMethodsReferenceJson = JsonSerializer.Serialize(
                _interopMethodsReference,
                interopMethodsReferenceJsonTypeInfo);
            var jsComponentParametersByIdentifierJson = JsonSerializer.Serialize(
                jsComponentInterop.Configuration.JSComponentParametersByIdentifier,
                WebRendererSerializerContext.Default.DictionaryStringJSComponentParameterArray);
            var jsComponentIdentifiersByInitializerJson = JsonSerializer.Serialize(
                jsComponentInterop.Configuration.JSComponentIdentifiersByInitializer,
                WebRendererSerializerContext.Default.DictionaryStringListString);

            var argsJson =
                $"[{rendererIdJson}, " +
                $"{interopMethodsReferenceJson}, " +
                $"{jsComponentParametersByIdentifierJson}, " +
                $"{jsComponentIdentifiersByInitializerJson}]";
            inProcessRuntime.InvokeJS(JSMethodIdentifier, argsJson, JSCallResultType.JSVoidResult, 0);
        }
        else
        {
            jsRuntime.InvokeVoidAsync(
                JSMethodIdentifier,
                _rendererId,
                _interopMethodsReference,
                jsComponentInterop.Configuration.JSComponentParametersByIdentifier,
                jsComponentInterop.Configuration.JSComponentIdentifiersByInitializer).Preserve();
        }
    }

    private static JsonTypeInfo<T> GetJsonTypeInfoFromJsonConverterFactories<T>(
        IList<JsonConverter> converters,
        JsonSerializerOptions optionsToUse)
    {
        foreach (var converter in converters)
        {
            if (converter is not JsonConverterFactory factory || !factory.CanConvert(typeof(T)))
            {
                continue;
            }

            if (factory.CreateConverter(typeof(T), optionsToUse) is not { } converterToUse)
            {
                continue;
            }

            return JsonMetadataServices.CreateValueInfo<T>(optionsToUse, converterToUse);
        }

        throw new InvalidOperationException($"Could not create a JsonTypeInfo for type {typeof(T).FullName}");
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

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Serialization,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Dictionary<string, JSComponentConfigurationStore.JSComponentParameter[]>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
internal sealed partial class WebRendererSerializerContext : JsonSerializerContext;
