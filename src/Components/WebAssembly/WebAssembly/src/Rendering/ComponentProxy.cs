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
        private readonly Type _componentType;

        private bool _disposed = false;

        public ComponentProxy(WebAssemblyRenderer renderer, int componentId, Type componentType)
        {
            _renderer = renderer;
            _componentId = componentId;
            _componentType = componentType;
        }

        [JSInvokable]
        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                return default;
            }

            _disposed = true;
            return new ValueTask(_renderer.RemoveDynamicComponentAsync(_componentId));
        }

        [JSInvokable]
        public ValueTask SetParametersAsync(IDictionary<string, object> parameters)
        {
            if (_disposed)
            {
                return default;
            }

            return default;
        }
    }
}
