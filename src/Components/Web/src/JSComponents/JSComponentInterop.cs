// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
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
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class JSComponentInterop : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, ParameterTypeCache> ParameterTypeCaches = new();

        static JSComponentInterop()
        {
            if (MetadataUpdater.IsSupported)
            {
                HotReloadManager.OnDeltaApplied += () => ParameterTypeCaches.Clear();
            }
        }

        private const int MaxParameters = 100;
        private readonly DotNetObjectReference<JSComponentInterop> _selfReference;
        private readonly Dictionary<string, Type> _allowedComponentTypes;
        private readonly JsonSerializerOptions _jsonOptions;
        private WebRenderer? _renderer;

        private WebRenderer Renderer => _renderer
            ?? throw new InvalidOperationException("This instance is not initialized.");

        /// <summary>
        /// Constructs an instance of <see cref="JSComponentInterop" />. This is only intended
        /// for use from framework code and should not be used directly from application code.
        /// </summary>
        /// <param name="configuration">The <see cref="JSComponentConfigurationStore" /></param>
        /// <param name="jsonOptions">The <see cref="JsonSerializerOptions" /></param>
        public JSComponentInterop(
            JSComponentConfigurationStore configuration,
            JsonSerializerOptions jsonOptions)
        {
            _selfReference = DotNetObjectReference.Create(this);
            _jsonOptions = jsonOptions;

            // Snapshot the config to ensure it's not mutated later
            _allowedComponentTypes = new(configuration.JsComponentTypesByIdentifier, StringComparer.Ordinal);
        }

        // This has to be internal and only called by WebRenderer (through a protected API) because,
        // by attaching a WebRenderer instance, you become able to call its protected internal APIs
        // such as AddRootComponent etc. and hence bypass the encapsulation. There should not be any
        // other way to attach a renderer to this instance.
        internal async ValueTask InitializeAsync(IJSRuntime jsRuntime, WebRenderer renderer)
        {
            if (_allowedComponentTypes.Count == 0)
            {
                return;
            }

            _renderer = renderer;

            await jsRuntime.InvokeVoidAsync(
                "Blazor._internal.setDynamicRootComponentManager",
                _selfReference);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public int AddRootComponent(string identifier, string domElementSelector)
        {
            if (!_allowedComponentTypes.TryGetValue(identifier, out var componentType))
            {
                throw new ArgumentException($"There is no registered JS component with identifier '{identifier}'.");
            }

            return Renderer.AddRootComponent(componentType, domElementSelector);
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

            var componentType = Renderer.GetRootComponentType(componentId);
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
            _ = Renderer.RenderRootComponentAsync(componentId, parameterViewBuilder.ToParameterView());
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        [JSInvokable]
        public void RemoveRootComponent(int componentId)
            => Renderer.RemoveRootComponent(componentId);

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
