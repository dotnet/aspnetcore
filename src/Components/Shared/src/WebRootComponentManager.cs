// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

#if COMPONENTS_SERVER
using ComponentTypeReference = Microsoft.AspNetCore.Components.ComponentTypeInfo;
#else
using ComponentTypeReference = System.Type;
#endif

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
        private Task _currentUpdateTask = Task.CompletedTask;

#if COMPONENTS_SERVER
        public Task AddRootComponentAsync(
            int ssrComponentId,
            [DynamicallyAccessedMembers(Component)] Type componentType,
            ComponentMarkerKey? key,
            WebRootComponentParameters parameters)
            => AddRootComponentAsync(
                ssrComponentId,
                renderer.ComponentTypeInfoResolver.GetRequiredTypeInfo(componentType),
                key,
                parameters);

        internal async Task AddRootComponentAsync(
            int ssrComponentId,
            ComponentTypeReference componentTypeInfo,
            ComponentMarkerKey? key,
            WebRootComponentParameters parameters)
#else
        public async Task AddRootComponentAsync(
            int ssrComponentId,
            [DynamicallyAccessedMembers(Component)] ComponentTypeReference componentTypeInfo,
            ComponentMarkerKey? key,
            WebRootComponentParameters parameters)
#endif
        {
#if COMPONENTS_SERVER
            if (_webRootComponents.Count + 1 > renderer._options.RootComponents.MaxJSRootComponents)
            {
                throw new InvalidOperationException("Exceeded the maximum number of allowed server interactive root components.");
            }
#endif

            if (_webRootComponents.ContainsKey(ssrComponentId))
            {
                throw new InvalidOperationException($"A root component with SSR component ID {ssrComponentId} already exists.");
            }

            var component = WebRootComponent.Create(renderer, componentTypeInfo, ssrComponentId, key, parameters);
            _webRootComponents.Add(ssrComponentId, component);
            await _currentUpdateTask;
            await component.RenderAsync(renderer);
        }

#if COMPONENTS_SERVER
        public Task UpdateRootComponentAsync(
            int ssrComponentId,
            [DynamicallyAccessedMembers(Component)] Type newComponentType,
            ComponentMarkerKey? newKey,
            WebRootComponentParameters newParameters)
            => UpdateRootComponentAsync(
                ssrComponentId,
                renderer.ComponentTypeInfoResolver.GetRequiredTypeInfo(newComponentType),
                newKey,
                newParameters);

        internal Task UpdateRootComponentAsync(
            int ssrComponentId,
            ComponentTypeReference newComponentTypeInfo,
            ComponentMarkerKey? newKey,
            WebRootComponentParameters newParameters)
#else
        public Task UpdateRootComponentAsync(
            int ssrComponentId,
            [DynamicallyAccessedMembers(Component)] ComponentTypeReference newComponentTypeInfo,
            ComponentMarkerKey? newKey,
            WebRootComponentParameters newParameters)
#endif
        {
            var component = GetRequiredWebRootComponent(ssrComponentId);
            return component.UpdateAsync(renderer, newComponentTypeInfo, newKey, newParameters, _currentUpdateTask);
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

#if COMPONENTS_SERVER
        internal IEnumerable<(int id, ComponentMarkerKey key, (ComponentTypeInfo componentTypeInfo, ParameterView parameters))> GetRootComponents()
        {
            foreach (var (id, (_, key, typeInfo, parameters)) in _webRootComponents)
            {
                yield return (id, key, (typeInfo, parameters));
            }
        }

#endif
        internal ComponentMarkerKey GetRootComponentKey(int componentId)
        {
            foreach (var (_, candidate) in _webRootComponents)
            {
                var (id, key, _, _) = candidate;
                if (id == componentId)
                {
                    return key;
                }
            }

            return default;
        }

        internal void SetCurrentUpdateTask(Task postStateTask)
        {
            _currentUpdateTask = postStateTask;
        }

        private sealed class WebRootComponent
        {
#if !COMPONENTS_SERVER
            [DynamicallyAccessedMembers(Component)]
#endif
            private readonly ComponentTypeReference _componentTypeInfo;
            private readonly string _ssrComponentIdString;
            private readonly ComponentMarkerKey _key;

            private WebRootComponentParameters _latestParameters;
            private int _interactiveComponentId;

            public static WebRootComponent Create(
                Renderer renderer,
#if !COMPONENTS_SERVER
                [DynamicallyAccessedMembers(Component)]
#endif
                ComponentTypeReference componentTypeInfo,
                int ssrComponentId,
                ComponentMarkerKey? key,
                WebRootComponentParameters initialParameters)
            {
                if (!key.HasValue)
                {
                    throw new InvalidOperationException("An invalid component marker key was provided.");
                }

                var ssrComponentIdString = ssrComponentId.ToString(CultureInfo.InvariantCulture);
                var interactiveComponentId = renderer.AddRootComponent(componentTypeInfo, ssrComponentIdString);

                return new(componentTypeInfo, ssrComponentIdString, key.Value, interactiveComponentId, initialParameters);
            }

            private WebRootComponent(
#if !COMPONENTS_SERVER
                [DynamicallyAccessedMembers(Component)]
#endif
                ComponentTypeReference componentTypeInfo,
                string ssrComponentIdString,
                ComponentMarkerKey key,
                int interactiveComponentId,
                in WebRootComponentParameters initialParameters)
            {
                _componentTypeInfo = componentTypeInfo;
                _ssrComponentIdString = ssrComponentIdString;
                _key = key;
                _interactiveComponentId = interactiveComponentId;
                _latestParameters = initialParameters;
            }

            public void Deconstruct(
                out int interactiveComponentId,
                out ComponentMarkerKey key,
                out ComponentTypeReference componentTypeInfo,
                out ParameterView parameters)
            {
                interactiveComponentId = _interactiveComponentId;
                key = _key;
                componentTypeInfo = _componentTypeInfo;
                parameters = _latestParameters.Parameters;
            }

            public Task UpdateAsync(
                Renderer renderer,
#if !COMPONENTS_SERVER
                [DynamicallyAccessedMembers(Component)]
#endif
                ComponentTypeReference newComponentTypeInfo,
                ComponentMarkerKey? newKey,
                WebRootComponentParameters newParameters,
                Task currentUpdateTask)
            {
                if (GetComponentType(_componentTypeInfo) != GetComponentType(newComponentTypeInfo))
                {
                    throw new InvalidOperationException("Cannot update components with mismatching types.");
                }

                if (!newKey.HasValue)
                {
                    throw new InvalidOperationException("An invalid component marker key was provided.");
                }

                if (!string.Equals(_key.LocationHash, newKey.Value.LocationHash, StringComparison.Ordinal) ||
                    !string.Equals(_key.FormattedComponentKey, newKey.Value.FormattedComponentKey, StringComparison.Ordinal))
                {
                    // The client should always supply updated parameters to a component with a matching key.
                    throw new InvalidOperationException("Cannot update components with mismatching keys.");
                }

                if (!string.IsNullOrEmpty(_key.FormattedComponentKey))
                {
                    // We can supply new parameters if the key has a @key value, because that means the client
                    // opted in to dynamic parameter updates.
                    _latestParameters = newParameters;
                    return RenderAsync(renderer, currentUpdateTask);
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
                        _interactiveComponentId = renderer.AddRootComponent(_componentTypeInfo, _ssrComponentIdString);
                        _latestParameters = newParameters;
                        return RenderAsync(renderer, currentUpdateTask);
                    }
                }
            }

            public Task RenderAsync(Renderer renderer)
            {
                return renderer.RenderRootComponentAsync(_interactiveComponentId, _latestParameters.Parameters);
            }

            public async Task RenderAsync(Renderer renderer, Task updateTask)
            {
                await updateTask;
                await renderer.RenderRootComponentAsync(_interactiveComponentId, _latestParameters.Parameters);
            }

            public void Remove(Renderer renderer)
            {
                renderer.RemoveRootComponent(_interactiveComponentId);
            }

            private static Type GetComponentType(ComponentTypeReference componentTypeInfo)
#if COMPONENTS_SERVER
                => componentTypeInfo.Type;
#else
                => componentTypeInfo;
#endif
        }
    }
}
