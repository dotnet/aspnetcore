// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.Json;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Infrastructure
{
    /// <summary>
    /// Provides JavaScript-callable interop methods that can add, update, or remove dynamic
    /// root components. This is intended for framework use only and should not be called
    /// directly from application code.
    /// </summary>
    public class DynamicRootComponentInterop : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, ParameterTypeCache> ParameterTypeCaches = new();

        static DynamicRootComponentInterop()
        {
            if (MetadataUpdater.IsSupported)
            {
                HotReloadManager.OnDeltaApplied += () => ParameterTypeCaches.Clear();
            }
        }

        private const int MaxParameters = 100;
        private readonly DotNetObjectReference<DynamicRootComponentInterop> _selfReference;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly Dictionary<string, Type> _allowedComponentTypes;
        private readonly WebRenderer _renderer;

        // This can't be publicly constructable because having a reference to it gives access to the
        // `protected internal` APIs on the renderer you pass in and bypasses the encapsulation.
        internal DynamicRootComponentInterop(
            DynamicRootComponentConfiguration configurationBuilder,
            WebRenderer renderer)
        {
            _selfReference = DotNetObjectReference.Create(this);
            _jsonOptions = configurationBuilder.JsonOptions;
            _renderer = renderer;

            // Snapshot the config to ensure it's not mutated later
            _allowedComponentTypes = new(configurationBuilder.AllowedComponentsByIdentifier, StringComparer.Ordinal);
        }

        internal async ValueTask InitializeAsync(IJSRuntime jsRuntime)
        {
            if (_allowedComponentTypes.Count > 0)
            {
                await jsRuntime.InvokeVoidAsync(
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

            return _renderer.AddRootComponent(componentType, domElementSelector);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void SetRootComponentParameters(int componentId, int parameterCount, byte[] parametersJsonUtf8)
        {
            // In case the client misreports the number of parameters, impose bounds so we know the amount
            // of work done is limited to a fixed, low amount.
            if (parameterCount < 0 || parameterCount > MaxParameters)
            {
                throw new ArgumentOutOfRangeException($"{nameof(parameterCount)} must be between 0 and {MaxParameters}.");
            }

            var componentType = _renderer.GetRootComponentType(componentId);
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
                            parameterValue = parametersReader.GetDouble();
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

            // This call gets back a task that represents the renderer reaching quiescence, but is not
            // used for async errors (there's a separate channel for errors, because renderer errors can
            // happen at any time due to component code). We don't want to expose quiescence info here
            // because there isn't a clear scenario for it, and it would lock down more implementation
            // details than we want. So, the task is not relevant to us, and we can safely discard it.
            _ = _renderer.RenderRootComponentAsync(componentId, parameterViewBuilder.ToParameterView());
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void RemoveRootComponent(int componentId)
            => _renderer.RemoveRootComponent(componentId);

        /// <inheritdoc />
        public void Dispose()
            => _selfReference.Dispose();

        private bool TryGetComponentParameterType(Type componentType, string parameterName, out Type parameterType)
        {
            var cacheForComponent = ParameterTypeCaches.GetOrAdd(componentType, static type => new ParameterTypeCache(type));
            return cacheForComponent.ParameterTypes.TryGetValue(parameterName, out parameterType!);
        }

        private readonly struct ParameterTypeCache
        {
            public readonly Dictionary<string, Type> ParameterTypes;

            public ParameterTypeCache(Type componentType)
            {
                ParameterTypes = new(StringComparer.OrdinalIgnoreCase);
                var candidateProperties = ComponentProperties.GetCandidateBindableProperties(componentType);
                foreach (var propertyInfo in candidateProperties)
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
