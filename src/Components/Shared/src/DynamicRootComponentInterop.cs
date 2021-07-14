// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Infrastructure
{
    // This type is a way of exposing some of the renderer methods to JS interop without
    // otherwise changing the public API surface.

    /// <summary>
    /// Contains methods called by interop. Intended for framework use only, not supported for use in application
    /// code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DynamicRootComponentInterop : IDisposable
    {
        // TODO: Clear on hot reload
        private static readonly ConcurrentDictionary<Type, ParameterTypeCache> ParameterTypeCaches = new();

        private const int MaxParameters = 100;
        private readonly DotNetObjectReference<DynamicRootComponentInterop> _selfReference;
        private readonly IJSRuntime _jsRuntime;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Func<int, Type> _getRootComponentType;
        private readonly Func<Type, string, int> _addRootComponent;
        private readonly Func<int, ParameterView, Task> _renderRootComponentAsync;
        private readonly Action<int> _removeRootComponent;
        private readonly Dictionary<string, Type> _allowedComponentTypes;

        internal DynamicRootComponentInterop(
            DefaultDynamicRootComponentConfiguration configurationBuilder,
            IJSRuntime jsRuntime,
            Func<int, Type> getRootComponentType,
            Func<Type, string, int> addRootComponent,
            Func<int, ParameterView, Task> renderRootComponentAsync,
            Action<int> removeRootComponent)
        {
            _selfReference = DotNetObjectReference.Create(this);
            _jsRuntime = jsRuntime;
            _jsonOptions = configurationBuilder.JsonOptions;
            _getRootComponentType = getRootComponentType;
            _addRootComponent = addRootComponent;
            _removeRootComponent = removeRootComponent;
            _renderRootComponentAsync = renderRootComponentAsync;

            // Snapshot the config to ensure it's not mutated later
            _allowedComponentTypes = new(configurationBuilder.AllowedComponentsByIdentifier, StringComparer.Ordinal);
        }

        internal async ValueTask InitializeAsync()
        {
            if (_allowedComponentTypes.Count > 0)
            {
                await _jsRuntime.InvokeVoidAsync(
                    "Blazor._internal.setDynamicRootComponentManager",
                    _selfReference);
            }
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public int AddRootComponent(string componentIdentifier, string domElementSelector)
        {
            if (!_allowedComponentTypes.TryGetValue(componentIdentifier, out var componentType))
            {
                throw new ArgumentException($"There is no registered dynamic root component with identifier '{componentIdentifier}'.");
            }

            return _addRootComponent(componentType, domElementSelector);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public Task RenderRootComponentAsync(int componentId, int parameterCount, byte[] parametersJsonUtf8)
        {
            // In case the client misreports the number of parameters, impose bounds so we know the amount
            // of work done is limited to a fixed, low amount.
            if (parameterCount < 0 || parameterCount > MaxParameters)
            {
                throw new ArgumentOutOfRangeException($"{nameof(parameterCount)} must be between 0 and {MaxParameters}.");
            }

            var componentType = _getRootComponentType(componentId);
            var parameterViewBuilder = new ParameterViewBuilder(parameterCount);

            var parametersReader = new Utf8JsonReader(parametersJsonUtf8);

            parametersReader.Read();
            Debug.Assert(parametersReader.TokenType == JsonTokenType.StartObject);

            parametersReader.Read();
            while (parametersReader.TokenType == JsonTokenType.PropertyName)
            {
                var parameterName = parametersReader.GetString()!;
                object? parameterValue;
                if (TryGetComponentParameterType(componentType, parameterName, out var parameterType))
                {
                    // It's a statically-declared parameter, so we can parse it into a known .NET type
                    parameterValue = JsonSerializer.Deserialize(
                        ref parametersReader,
                        parameterType,
                        _jsonOptions);
                }
                else
                {
                    // Unknown parameter - possibly valid as "catch-all". Use whatever type appears
                    // to be present in the JSON data.
                    parametersReader.Read();
                    switch (parametersReader.TokenType)
                    {
                        case JsonTokenType.Number:
                            parameterValue = parametersReader.GetInt64();
                            break;
                        case JsonTokenType.String:
                            parameterValue = parametersReader.GetString();
                            break;
                        case JsonTokenType.True:
                        case JsonTokenType.False:
                            parameterValue = parametersReader.GetBoolean();
                            break;
                        case JsonTokenType.Null:
                            parameterValue = null;
                            break;
                        default:
                            throw new ArgumentException($"There is no declared parameter named '{parameterName}', so the supplied object cannot be deserialized.");
                    }
                }

                parameterViewBuilder.Add(parameterName, parameterValue);
                parametersReader.Read();
            }

            return _renderRootComponentAsync(componentId, parameterViewBuilder.ToParameterView());
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void RemoveRootComponent(int componentId)
            => _removeRootComponent(componentId);

        /// <inheritdoc />
        public void Dispose()
            => _selfReference.Dispose();

        private bool TryGetComponentParameterType(Type componentType, string parameterName, out Type parameterType)
        {
            var cache = ParameterTypeCaches.GetOrAdd(componentType, static type => new ParameterTypeCache(type));
            return cache.ParameterTypes.TryGetValue(parameterName, out parameterType!);
        }

        private readonly struct ParameterTypeCache
        {
            public readonly Dictionary<string, Type> ParameterTypes;

            public ParameterTypeCache(Type componentType)
            {
                ParameterTypes = new(StringComparer.OrdinalIgnoreCase);
                foreach (var propertyInfo in componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (propertyInfo.IsDefined(typeof(ParameterAttribute)))
                    {
                        ParameterTypes.Add(propertyInfo.Name, propertyInfo.PropertyType);
                    }
                }
            }
        }
    }
}
