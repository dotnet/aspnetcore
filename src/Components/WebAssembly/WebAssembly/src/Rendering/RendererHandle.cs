using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    internal class RendererHandle
    {
        private readonly RootComponentTypeCache _rootComponentTypeCache;
        private readonly WebAssemblyRenderer _renderer;
        private readonly IJSRuntime _jsRuntime;
        private DotNetObjectReference<RendererHandle>? _thisAsDotNetObjectRef;

        public RendererHandle(WebAssemblyRenderer renderer, RootComponentTypeCache rootComponentTypeCache, IJSRuntime jsRuntime)
        {
            _renderer = renderer;
            _rootComponentTypeCache = rootComponentTypeCache;
            _jsRuntime = (IJSInProcessRuntime)jsRuntime;
        }

        [JSInvokable]
        public Task<DotNetObjectReference<ComponentProxy>> RenderRootComponent(DescriptorOrAlias typeOrAlias, string selector, IDictionary<string, object> parameters)
        {
            if (typeOrAlias is null)
            {
                throw new ArgumentNullException(nameof(typeOrAlias));
            }

            if (string.IsNullOrEmpty(selector))
            {
                throw new ArgumentException($"'{nameof(selector)}' cannot be null or empty.", nameof(selector));
            }

            var componentType = typeOrAlias.IsDescriptor() ?
                _rootComponentTypeCache.GetRootComponent(typeOrAlias.Descriptor!.Assembly!, typeOrAlias.Descriptor!.TypeName!) :
                throw new InvalidOperationException("Alias not supported yet.");

            if (componentType == null)
            {
                throw new InvalidOperationException("Could not find dynamic component type.");
            }

            var parameterView = parameters == null || parameters.Count == 0 ? ParameterView.Empty : DeserializeParameters(componentType, parameters);

            var proxy = _renderer.AddDynamicComponentAsync(componentType, selector, parameterView);
            // TODO: This method doesn't actually have to be async?
            // We don't want to wait until the rendering has completed fully to return the proxy, since the component might need to be removed
            // before its done with setParameters.
            return Task.FromResult(DotNetObjectReference.Create(proxy));
        }

        private ParameterView DeserializeParameters(Type? componentType, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        internal record DescriptorOrAlias
        {
            public const string DescriptorKind = "Descriptor";
            public const string AliasKind = "Alias";

            public bool IsDescriptor() => string.Equals(Kind, DescriptorKind, StringComparison.OrdinalIgnoreCase);

            public bool IsAlias() => string.Equals(Kind, Alias, StringComparison.OrdinalIgnoreCase);

            public string? Kind;
            public string? Alias;
            public ComponentDescriptor? Descriptor;
        }

        internal record ComponentDescriptor
        {
            public string? Assembly;
            public string? TypeName;
        }

        internal ValueTask Initialize()
        {
            if (_thisAsDotNetObjectRef != null)
            {
                throw new InvalidOperationException("Renderer handle already initialized.");
            }

            _thisAsDotNetObjectRef = DotNetObjectReference.Create(this);
            return _jsRuntime.InvokeVoidAsync("Blazor._internal.setComponentRenderer", _thisAsDotNetObjectRef);
        }
    }
}
