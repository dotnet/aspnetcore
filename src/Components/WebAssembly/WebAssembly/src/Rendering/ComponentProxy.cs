using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    internal class ComponentProxy : IAsyncDisposable
    {
        private readonly WebAssemblyRenderer _renderer;
        private readonly int _componentId;

        private bool _disposed;
        private DynamicComponentParameterDeserializer? _deserializer;

        public ComponentProxy(WebAssemblyRenderer renderer, string selector, int componentId)
        {
            _renderer = renderer;
            Selector = selector;
            _componentId = componentId;
        }

        public string Selector { get; }

        [JSInvokable]
        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return default;
            }

            _disposed = true;
            return new ValueTask(_renderer.RemoveDynamicComponentAsync(_componentId));
        }

        [JSInvokable]
        public Task SetParametersAsync(IDictionary<string, object> parameters)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_deserializer == null)
            {
                throw new InvalidOperationException("Parameter deserializer not initialized.");
            }

            var parameterView = _deserializer.DeserializeParameters(parameters);

            return _renderer.SetDynamicComponentParameters(_componentId, parameterView);
        }

        internal void SetParameterDeserializer(DynamicComponentParameterDeserializer deserializer)
        {
            if(_deserializer != null)
            {
                throw new InvalidOperationException("Parameter deserializer already initialized.");
            }

            _deserializer = deserializer;
        }
    }
}
