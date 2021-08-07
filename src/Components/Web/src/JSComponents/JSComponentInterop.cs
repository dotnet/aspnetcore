// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
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
    public class JSComponentInterop
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
        private WebRenderer? _renderer;

        internal JSComponentConfigurationStore Configuration { get; }

        private WebRenderer Renderer => _renderer
            ?? throw new InvalidOperationException("This instance is not initialized.");

        /// <summary>
        /// Constructs an instance of <see cref="JSComponentInterop" />. This is only intended
        /// for use from framework code and should not be used directly from application code.
        /// </summary>
        /// <param name="configuration">The <see cref="JSComponentConfigurationStore" /></param>
        public JSComponentInterop(JSComponentConfigurationStore configuration)
        {
            Configuration = configuration;
        }

        // This has to be internal and only called by WebRenderer (through a protected API) because,
        // by attaching a WebRenderer instance, you become able to call its protected internal APIs
        // such as AddRootComponent etc. and hence bypass the encapsulation. There should not be any
        // other way to attach a renderer to this instance.
        internal void AttachToRenderer(WebRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        protected internal virtual int AddRootComponent(string identifier, string domElementSelector)
        {
            if (!Configuration.JsComponentTypesByIdentifier.TryGetValue(identifier, out var componentType))
            {
                throw new ArgumentException($"There is no registered JS component with identifier '{identifier}'.");
            }

            return Renderer.AddRootComponent(componentType, domElementSelector);
        }

        /// <summary>
        /// For framework use only.
        /// </summary>
        protected internal void SetRootComponentParameters(int componentId, int parameterCount, JsonElement parametersJson, JsonSerializerOptions jsonOptions)
        {
            // In case the client misreports the number of parameters, impose bounds so we know the amount
            // of work done is limited to a fixed, low amount.
            if (parameterCount < 0 || parameterCount > MaxParameters)
            {
                throw new ArgumentOutOfRangeException($"{nameof(parameterCount)} must be between 0 and {MaxParameters}.");
            }

            var componentType = Renderer.GetRootComponentType(componentId);
            var parameterViewBuilder = new ParameterViewBuilder(parameterCount);

            var parametersJsonEnumerator = parametersJson.EnumerateObject();
            foreach (var jsonProperty in parametersJsonEnumerator)
            {
                var parameterName = jsonProperty.Name;
                var parameterJsonValue = jsonProperty.Value;
                object? parameterValue;
                if (TryGetComponentParameterType(componentType, parameterName, out var parameterType))
                {
                    if (parameterType == typeof(EventCallback))
                    {
                        var jsObjectReference = JsonSerializer.Deserialize<IJSObjectReference>(parameterJsonValue, jsonOptions)!;
                        var eventCallbackRelay = new JSEventCallbackRelay(jsObjectReference);
                        parameterValue = eventCallbackRelay.Callback;
                    }
                    else
                    {
                        // It's a statically-declared parameter, so we can parse it into a known .NET type
                        parameterValue = JsonSerializer.Deserialize(
                            parameterJsonValue,
                            parameterType,
                            jsonOptions);
                    }
                }
                else
                {
                    // Unknown parameter - possibly valid as "catch-all". Use whatever type appears
                    // to be present in the JSON data.
                    switch (parameterJsonValue.ValueKind)
                    {
                        case JsonValueKind.Number:
                            parameterValue = parameterJsonValue.GetDouble();
                            break;
                        case JsonValueKind.String:
                            parameterValue = parameterJsonValue.GetString();
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            parameterValue = parameterJsonValue.GetBoolean();
                            break;
                        case JsonValueKind.Null:
                        case JsonValueKind.Undefined:
                            parameterValue = null;
                            break;
                        default:
                            throw new ArgumentException($"There is no declared parameter named '{parameterName}', so the supplied object cannot be deserialized.");
                    }
                }

                parameterViewBuilder.Add(parameterName, parameterValue);
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
        protected internal virtual void RemoveRootComponent(int componentId)
            => Renderer.RemoveRootComponent(componentId);

        internal static ParameterTypeCache GetComponentParameters(Type componentType)
            => ParameterTypeCaches.GetOrAdd(componentType, static type => new ParameterTypeCache(type));

        private static bool TryGetComponentParameterType(Type componentType, string parameterName, out Type parameterType)
        {
            var cacheForComponent = GetComponentParameters(componentType);
            return cacheForComponent.ParameterTypes.TryGetValue(parameterName, out parameterType!);
        }

        internal readonly struct ParameterTypeCache
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
