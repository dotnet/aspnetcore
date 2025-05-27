// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal partial class CircuitPersistenceManager(
    ServerComponentSerializer serverComponentSerializer,
    ICircuitPersistenceProvider circuitPersistenceProvider,
    IDataProtectionProvider dataProtectionProvider)
{

    public async Task PauseCircuitAsync(CircuitHost circuit, CancellationToken cancellation = default)
    {
        var renderer = circuit.Renderer;
        var persistenceManager = circuit.Services.GetRequiredService<ComponentStatePersistenceManager>();
        using var subscription = persistenceManager.State.RegisterOnPersisting(
            () => PersistRootComponents(renderer, persistenceManager.State),
            RenderMode.InteractiveServer);
        var store = new CircuitPersistenceManagerStore();
        await persistenceManager.PersistStateAsync(store, renderer);

        await circuitPersistenceProvider.PersistCircuitAsync(
            circuit.CircuitId,
            store.PersistedCircuitState,
            cancellation);
    }

    private Task PersistRootComponents(RemoteRenderer renderer, PersistentComponentState state)
    {
        var persistedComponents = new Dictionary<int, ComponentMarker>();
        var components = renderer.GetOrCreateWebRootComponentManager().GetRootComponents();
        var invocation = new ServerComponentInvocationSequence();
        foreach (var (id, componentKey, (componentType, parameters)) in components)
        {
            var marker = ComponentMarker.Create(ComponentMarker.ServerMarkerType, false, componentKey);
            serverComponentSerializer.SerializeInvocation(ref marker, invocation, componentType, parameters);
            persistedComponents.Add(id, marker);
        }

        state.PersistAsJson(typeof(CircuitPersistenceManager).FullName, persistedComponents);

        return Task.CompletedTask;
    }

    public async Task<PersistedCircuitState> ResumeCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
    {
        return await circuitPersistenceProvider.RestoreCircuitAsync(circuitId, cancellation);
    }

    internal PersistedCircuitState FromProtectedState(string rootComponents, string applicationState) => throw new NotImplementedException();

    // We are going to construct a RootComponentOperationBatch but we are going to replace the descriptors from the client with the
    // descriptors that we have persisted when pausing the circuit.
    // The way pausing and resuming works is that when the client starts the resume process, it 'simulates' that an SSR has happened and
    // queues and 'Add' operation for each server-side component that is on the document.
    // That ends up calling UpdateRootComponents with the old descriptors and no application state.
    // On the server side, we replace the descriptors with the ones that we have persisted. We can't use the original descriptors because
    // those have a lifetime of ~ 5 minutes, after which we are not able to unprotect them anymore.
    internal RootComponentOperationBatch ToRootComponentOperationBatch(
        IServerComponentDeserializer serverComponentDeserializer,
        byte[] rootComponents,
        string serializedComponentOperations)
    {
        // Deserialize the existing batch the client has sent but ignore the markers
        if (!serverComponentDeserializer.TryDeserializeRootComponentOperations(
            serializedComponentOperations,
            out var result,
            deserializeDescriptors: false))
        {
            return null;
        }

        var data = JsonSerializer.Deserialize(
            rootComponents,
            CircuitPersistenceManagerSerializerContext.Default.DictionaryInt32ComponentMarker);

        // Ensure that all operations in the batch are `Add` operations.
        for (var i = 0; i < result.Operations.Length; i++)
        {
            var operation = result.Operations[i];
            if (operation.Type != RootComponentOperationType.Add)
            {
                return null;
            }

            // Retrieve the marker from the persisted root components, replace it and deserialize the descriptor
            if (!data.TryGetValue(operation.SsrComponentId, out var marker))
            {
                return null;
            }
            operation.Marker = marker;

            if (!serverComponentDeserializer.TryDeserializeWebRootComponentDescriptor(operation.Marker.Value, out var descriptor))
            {
                return null;
            }

            operation.Descriptor = descriptor;
        }

        return result;
    }

    internal (string rootComponents, string applicationState) ToProtectedState(PersistedCircuitState state) => throw new NotImplementedException();

    internal ProtectedPrerenderComponentApplicationStore ToComponentApplicationStore(Dictionary<string, byte[]> applicationState)
    {
        return new ProtectedPrerenderComponentApplicationStore(applicationState, dataProtectionProvider);
    }

    private class CircuitPersistenceManagerStore : IPersistentComponentStateStore
    {
        internal PersistedCircuitState PersistedCircuitState { get; private set; }

        // This store only support serializing the state
        Task<IDictionary<string, byte[]>> IPersistentComponentStateStore.GetPersistedStateAsync() => throw new NotImplementedException();

        // During the persisting phase the state is captured into a Dictionary<string, byte[]>, our implementation registers
        // a callback so that it can run at the same time as the other components' state is persisted.
        // We then are called to save the persisted state, at which point, we extract the component records
        // and store them separately from the other state.
        Task IPersistentComponentStateStore.PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            var dictionary = new Dictionary<string, byte[]>(state.Count - 1);
            byte[] rootComponentMarkers = null;
            foreach (var (key, value) in state)
            {
                if (key == typeof(CircuitPersistenceManager).FullName)
                {
                    rootComponentMarkers = value;
                }
                else
                {
                    dictionary[key] = value;
                }
            }

            PersistedCircuitState = new PersistedCircuitState
            {
                ApplicationState = dictionary,
                RootComponents = rootComponentMarkers
            };

            return Task.CompletedTask;
        }
    }

    [JsonSerializable(typeof(Dictionary<int, ComponentMarker>))]
    internal partial class CircuitPersistenceManagerSerializerContext : JsonSerializerContext
    {
    }
}
