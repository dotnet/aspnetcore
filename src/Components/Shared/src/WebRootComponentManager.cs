// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        private readonly Dictionary<int, WebRootComponentInfo> _webRootComponentInfo = new();

        public Task AddRootComponentAsync(
            [DynamicallyAccessedMembers(Component)] Type componentType,
            ParameterView parameters,
            string key,
            string domElementSelector)
        {
            if (!BoundaryMarkerKey.TryParse(key.AsMemory(), out var boundaryMarkerKey))
            {
                throw new InvalidOperationException($"The boundary marker key '{boundaryMarkerKey}' had an invalid format.");
            }

            var componentId = renderer.AddRootComponent(componentType, domElementSelector);
            var canSupplyNewParameters = boundaryMarkerKey.HasComponentKey;
            _webRootComponentInfo[componentId] = new(key, parameters, canSupplyNewParameters);

            return renderer.RenderRootComponentAsync(componentId, parameters);
        }

        public Task UpdateRootComponentAsync(
            int componentId,
            [DynamicallyAccessedMembers(Component)] Type componentType,
            ParameterView newParameters,
            string key,
            string domElementSelector)
        {
            if (!_webRootComponentInfo.TryGetValue(componentId, out var component))
            {
                // TODO: The client should get confirmation about whether a component was replaced or re-rendered
                // before sending additional updates. Otherwise, it may send updates to a removed component without
                // realizing it was replaced by a component with a new component ID.
                throw new InvalidOperationException(
                    "Attempted to update a root component that either doesn't exist yet or was previously disposed.");
            }

            if (!string.Equals(key, component.Key, StringComparison.Ordinal))
            {
                // The client should always supply updated parameters to a component with a matching key, even if the key is null.
                throw new InvalidOperationException("Cannot update components with mismatching keys.");
            }

            if (component.CanSupplyNewParameters)
            {
                return renderer.RenderRootComponentAsync(componentId, newParameters);
            }
            else
            {
                if (component.InitialParameters.DefinitelyEquals(newParameters))
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
                    RemoveRootComponent(componentId);
                    return AddRootComponentAsync(componentType, newParameters, key, domElementSelector);
                }
            }
        }

        public void RemoveRootComponent(int componentId)
        {
            _webRootComponentInfo.Remove(componentId);
            renderer.RemoveRootComponent(componentId);
        }

        private readonly record struct WebRootComponentInfo(
            string Key,
            ParameterView InitialParameters,
            bool CanSupplyNewParameters);
    }
}
