// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal partial class RemoteRenderer
{
    private WebRootComponentManager? _webRootComponentManager;

    public WebRootComponentManager GetOrCreateWebRootComponentManager()
        => _webRootComponentManager ??= new(this);

    // Manages components that get added, updated, or removed in Blazor Web scenarios
    // via Blazor endpoint invocations.
    public sealed class WebRootComponentManager(RemoteRenderer renderer)
    {
        private readonly Dictionary<int, RootComponentSnapshot> _latestRootComponentSnapshots = new();
        private HashSet<int> _componentsFromPreviousInvocation = new();
        private HashSet<int> _componentsFromCurrentInvocation = new();

        // Should be called before handling descriptors from a new endpoint invocation.
        public void NewInvocationHasStarted()
        {
            // The last known "current" invocation is now the old invocation.
            // We'll start with an empty set of components representing the new invocation,
            // moving components from the old set to the new set as we process operations.
            // At the end, the new set will contain all components that are still current.
            // The old set should then be empty. If it isn't, that's an error, because it means
            // some components from the old invocation weren't updated or removed.
            Debug.Assert(_componentsFromPreviousInvocation.Count == 0);
            (_componentsFromPreviousInvocation, _componentsFromCurrentInvocation) = (_componentsFromCurrentInvocation, _componentsFromPreviousInvocation);
        }

        // Should be called as soon as we think state from a previous invocation should be considered stale.
        public void PreviousInvocationIsStale()
        {
            if (_componentsFromPreviousInvocation.Count != 0)
            {
                throw new InvalidOperationException(
                    "Failed to update the set of interactive root components because components that weren't included in the most recent " +
                    "response were not correctly disposed.");
            }
        }

        public Task AddRootComponentAsync(Type componentType, ParameterView parameters, string? key, string domElementSelector)
        {
            var componentId = renderer.AddRootComponent(componentType, domElementSelector);

            _componentsFromCurrentInvocation.Add(componentId);
            _latestRootComponentSnapshots[componentId] = new(parameters, key);

            return renderer.RenderRootComponentAsync(componentId, parameters);
        }

        public Task UpdateRootComponentAsync(int componentId, Type componentType, ParameterView newParameters, string? key, string domElementSelector)
        {
            if (!_latestRootComponentSnapshots.TryGetValue(componentId, out var snapshot))
            {
                // TODO: The client should get confirmation about whether a component was replaced or re-rendered
                // before sending additional updates. Otherwise, it may send updates to a removed component without
                // realizing it was replaced by a component with a new component ID.
                throw new InvalidOperationException("Attempted to update a root component that either doesn't exist or was previously disposed.");
            }

            if (key is not null && string.Equals(key, snapshot.Key, StringComparison.Ordinal))
            {
                _componentsFromPreviousInvocation.Remove(componentId);
                _componentsFromCurrentInvocation.Add(componentId);
                return renderer.RenderRootComponentAsync(componentId, newParameters);
            }

            if (snapshot.Parameters.DefinitelyEquals(newParameters))
            {
                // The parameters haven't changed, so there's no work to do.
                _componentsFromPreviousInvocation.Remove(componentId);
                _componentsFromCurrentInvocation.Add(componentId);
                return Task.CompletedTask;
            }
            else
            {
                // The component parameters have changed. Rather than update the existing instance, we'll dispose
                // it and replace it with a new one. This is because it's the client's choice how to
                // match prerendered components with existing components, and we don't want to allow
                // clients to maliciously assign parameters to the wrong component.
                RemoveRootComponent(componentId);
                return AddRootComponentAsync(componentType, newParameters, domElementSelector, key);
            }
        }

        public void RemoveRootComponent(int componentId)
        {
            _componentsFromPreviousInvocation.Remove(componentId);
            _componentsFromCurrentInvocation.Remove(componentId);
            _latestRootComponentSnapshots.Remove(componentId);

            renderer.RemoveRootComponent(componentId);
        }

        private readonly record struct RootComponentSnapshot(ParameterView Parameters, string? Key);
    }
}
