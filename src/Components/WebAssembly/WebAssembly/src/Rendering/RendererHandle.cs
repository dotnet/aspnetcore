using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    internal class RendererHandle
    {
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly WebAssemblyRenderer _renderer;
        private readonly DynamicComponentCollection _dynamicComponents;
        private DotNetObjectReference<RendererHandle>? _thisAsDotNetObjectRef;

        public RendererHandle(
            WebAssemblyRenderer renderer,
            DynamicComponentCollection dynamicComponents,
            JsonSerializerOptions serializerOptions)
        {
            _renderer = renderer;
            _dynamicComponents = dynamicComponents;
            _serializerOptions = serializerOptions;
        }

        [JSInvokable]
        public Task<DotNetObjectReference<ComponentProxy>> RenderRootComponent(
            string elementName,
            string selector,
            IJSObjectReference eventRaiser,
            IDictionary<string, object> parameters)
        {
            if (elementName is null)
            {
                throw new ArgumentNullException(nameof(elementName));
            }

            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException($"'{nameof(selector)}' cannot be null or empty.", nameof(selector));
            }

            var componentDefinition = _dynamicComponents.GetByName(elementName);

            if (componentDefinition == null)
            {
                throw new InvalidOperationException($"Could not find dynamic component with name '{elementName}'.");
            }

            var deserializer = new DynamicComponentParameterDeserializer(componentDefinition, eventRaiser, _serializerOptions);

            var parameterView = parameters == null || parameters.Count == 0 ? ParameterView.Empty : deserializer.DeserializeParameters(parameters);

            var proxy = _renderer.AddDynamicComponentAsync(componentDefinition.ComponentType, selector, parameterView);
            proxy.SetParameterDeserializer(deserializer);
            // TODO: This method doesn't actually have to be async?
            // We don't want to wait until the rendering has completed fully to return the proxy, since the component might need to be removed
            // before its done with setParameters.
            return Task.FromResult(DotNetObjectReference.Create(proxy));
        }

        internal ValueTask Initialize(IJSRuntime jsRuntime)
        {
            if (_thisAsDotNetObjectRef != null)
            {
                throw new InvalidOperationException("Renderer handle already initialized.");
            }

            _thisAsDotNetObjectRef = DotNetObjectReference.Create(this);
            return jsRuntime.InvokeVoidAsync("Blazor._internal.setComponentRenderer", _thisAsDotNetObjectRef);
        }
    }

    internal class DynamicComponentParameterDeserializer
    {
        private readonly DynamicComponentDefinition? _componentDefinition;
        private readonly IJSObjectReference? _eventRaiser;
        private readonly JsonSerializerOptions? _serializerOptions;

        public DynamicComponentParameterDeserializer(
            DynamicComponentDefinition componentDefinition,
            IJSObjectReference eventRaiser,
            JsonSerializerOptions serializerOptions)
        {
            _componentDefinition = componentDefinition;
            _eventRaiser = eventRaiser;
            _serializerOptions = serializerOptions;
        }

        public ParameterView DeserializeParameters(IDictionary<string, object> parameters)
        {
            if (_componentDefinition == null || _eventRaiser == null || _serializerOptions == null)
            {
                throw new InvalidOperationException("Parameter deserializer not initialized.");
            }

            var extraParameters = _componentDefinition.HasCatchAllProperty ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;
            // Could we just reuse the dictionary?
            var deserializedParameters = new Dictionary<string, object?>();
            foreach (var (name, value) in parameters)
            {
                var parameterDefinition = _componentDefinition.GetParameter(name);
                if (parameterDefinition != null)
                {
                    if (!parameterDefinition.IsCallback)
                    {
                        var actualValue = JsonSerializer.Deserialize(
                            ((JsonElement)value).GetRawText(),
                            parameterDefinition.ParameterType,
                            _serializerOptions);

                        deserializedParameters.Add(name, actualValue);
                    }
                    else
                    {
                    }

                    continue;
                }

                if (extraParameters != null)
                {
                    extraParameters.Add(name);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid additional parameter '{name}'");
                }
            }

            if (_componentDefinition.HasCallbackParameter)
            {
                foreach (var definition in _componentDefinition.Parameters)
                {
                    var type = definition.ParameterType;
                    if (definition.IsCallback && !deserializedParameters.ContainsKey(definition.Name))
                    {
                        if (type == typeof(EventCallback))
                        {
                            deserializedParameters.Add(definition.Name, new EventCallback(null, new Func<Task>(async () => await _eventRaiser.InvokeVoidAsync("raiseEvent", definition.Name))));
                        }
                    }
                }
            }

            if (extraParameters != null)
            {
                foreach (var parameterName in extraParameters)
                {
                    var value = (JsonElement)parameters[parameterName];
                    object? parsedValue = value.ValueKind switch
                    {
                        JsonValueKind.Undefined => default,
                        JsonValueKind.Object => throw new InvalidOperationException("Can't deserialize unregistered additional object parameters."),
                        JsonValueKind.Array => throw new InvalidOperationException("Can't deserialize unregistered additional object parameters."),
                        JsonValueKind.String => value.GetString(),
                        JsonValueKind.Number when value.TryGetInt64(out var natural) => natural,
                        JsonValueKind.Number when value.TryGetDouble(out var real) => real,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => default,
                        _ => throw new InvalidOperationException("Unknown JsonElement kind"),
                    };
                    deserializedParameters.Add(parameterName, parsedValue);
                }
            }

            return ParameterView.FromDictionary(deserializedParameters);
        }
    }
}
