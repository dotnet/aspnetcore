// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

#if COMPONENTS_SERVER
namespace Microsoft.AspNetCore.Components.Server.Circuits;

using Renderer = RemoteRenderer;

internal partial class RemoteRenderer
#elif COMPONENTS_WEBASSEMBLY
namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

using Renderer = WebAssemblyRenderer;

internal partial class WebAssemblyRenderer
#else
#error WebRootComponentManager cannot be defined in this assembly.
#endif
{
    private WebRootComponentManager? _webRootComponentManager;

    public WebRootComponentManager GetOrCreateWebRootComponentManager()
        => _webRootComponentManager ??= new(this);

    // Manages components that get added, updated, or removed in Blazor Web scenarios
    // via Blazor endpoint invocations.
    public sealed class WebRootComponentManager(Renderer renderer)
    {
        private readonly Dictionary<int, WebRootComponent> _webRootComponents = new();

        public async Task AddRootComponentAsync(
            int ssrComponentId,
            [DynamicallyAccessedMembers(Component)] Type componentType,
            string key,
            WebRootComponentParameters parameters)
        {
#if COMPONENTS_SERVER
            if (_webRootComponents.Count + 1 > renderer._options.RootComponents.MaxInteractiveServerRootComponentCount)
            {
                throw new InvalidOperationException("Exceeded the maximum number of allowed server interactive root components.");
            }
#endif

            if (_webRootComponents.ContainsKey(ssrComponentId))
            {
                throw new InvalidOperationException($"A root component with SSR component ID {ssrComponentId} already exists.");
            }

            var component = await WebRootComponent.CreateAndRenderAsync(renderer, componentType, ssrComponentId, key, parameters);

            _webRootComponents.Add(ssrComponentId, component);
        }

        public Task UpdateRootComponentAsync(
            int ssrComponentId,
            string newKey,
            WebRootComponentParameters newParameters)
        {
            var component = GetRequiredWebRootComponent(ssrComponentId);
            return component.UpdateAsync(renderer, newKey, newParameters);
        }

        public void RemoveRootComponent(int ssrComponentId)
        {
            var component = GetRequiredWebRootComponent(ssrComponentId);
            component.Remove(renderer);
            _webRootComponents.Remove(ssrComponentId);
        }

        private WebRootComponent GetRequiredWebRootComponent(int ssrComponentId)
        {
            if (!_webRootComponents.TryGetValue(ssrComponentId, out var component))
            {
                throw new InvalidOperationException($"No root component exists with SSR component ID {ssrComponentId}.");
            }

            return component;
        }

        private sealed class WebRootComponent
        {
            [DynamicallyAccessedMembers(Component)]
            private readonly Type _componentType;
            private readonly string _ssrComponentIdString;
            private readonly string _key;
            private readonly bool _canSupplyNewParameters;

            private WebRootComponentParameters _latestParameters;
            private int _interactiveComponentId;

            public static async Task<WebRootComponent> CreateAndRenderAsync(
                Renderer renderer,
                [DynamicallyAccessedMembers(Component)] Type componentType,
                int ssrComponentId,
                string key,
                WebRootComponentParameters initialParameters)
            {
                if (!BoundaryMarkerKey.TryParse(key.AsMemory(), out var boundaryMarkerKey))
                {
                    throw new InvalidOperationException($"The key '{key}' had an invalid format.");
                }

                var ssrComponentIdString = ssrComponentId.ToString(CultureInfo.InvariantCulture);
                var interactiveComponentId = renderer.AddRootComponent(componentType, ssrComponentIdString);
                var canSupplyNewParameters = boundaryMarkerKey.HasComponentKey;

                await renderer.RenderRootComponentAsync(interactiveComponentId, initialParameters.Parameters);

                return new(componentType, ssrComponentIdString, key, canSupplyNewParameters, interactiveComponentId, initialParameters);
            }

            private WebRootComponent(
                [DynamicallyAccessedMembers(Component)] Type componentType,
                string ssrComponentIdString,
                string key,
                bool canSupplyNewParameters,
                int interactiveComponentId,
                in WebRootComponentParameters initialParameters)
            {
                _componentType = componentType;
                _ssrComponentIdString = ssrComponentIdString;
                _key = key;
                _canSupplyNewParameters = canSupplyNewParameters;
                _interactiveComponentId = interactiveComponentId;
                _latestParameters = initialParameters;
            }

            public Task UpdateAsync(
                Renderer renderer,
                string newKey,
                WebRootComponentParameters newParameters)
            {
                if (!string.Equals(newKey, _key, StringComparison.Ordinal))
                {
                    // The client should always supply updated parameters to a component with a matching key.
                    throw new InvalidOperationException("Cannot update components with mismatching keys.");
                }

                if (_canSupplyNewParameters)
                {
                    _latestParameters = newParameters;
                    return renderer.RenderRootComponentAsync(_interactiveComponentId, _latestParameters.Parameters);
                }
                else
                {
                    if (_latestParameters.DefinitelyEquals(newParameters))
                    {
                        // The parameters haven't changed, so there's no work to do.
                        return Task.CompletedTask;
                    }
                    else
                    {
                        // The component parameters have changed. Rather than update the existing instance, we'll dispose
                        // it and replace it with a new one. This is because it's the client's choice how to
                        // match prerendered components with existing components, and we don't want to allow
                        // clients to maliciously assign parameters to the wrong component.
                        renderer.RemoveRootComponent(_interactiveComponentId);
                        _interactiveComponentId = renderer.AddRootComponent(_componentType, _ssrComponentIdString);
                        _latestParameters = newParameters;
                        return renderer.RenderRootComponentAsync(_interactiveComponentId, _latestParameters.Parameters);
                    }
                }
            }

            public void Remove(Renderer renderer)
            {
                renderer.RemoveRootComponent(_interactiveComponentId);
            }
        }
    }
}
