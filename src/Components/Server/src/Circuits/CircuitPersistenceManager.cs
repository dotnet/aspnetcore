// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal partial class CircuitPersistenceManager(
    IOptions<CircuitOptions> circuitOptions,
    ServerComponentSerializer serverComponentSerializer,
    ICircuitPersistenceProvider circuitPersistenceProvider,
    IDataProtectionProvider dataProtectionProvider)
{
    public async Task PauseCircuitAsync(CircuitHost circuit, bool saveStateToClient = false, CancellationToken cancellation = default)
    {
        var renderer = circuit.Renderer;
        var persistenceManager = circuit.Services.GetRequiredService<ComponentStatePersistenceManager>();
        var collector = new CircuitPersistenceManagerCollector(circuitOptions, serverComponentSerializer, circuit.Renderer);
        using var subscription = persistenceManager.State.RegisterOnPersisting(
            collector.PersistRootComponents,
            RenderMode.InteractiveServer);

        await persistenceManager.PersistStateAsync(collector, renderer);

        if (saveStateToClient)
        {
            await SaveStateToClient(circuit, collector.PersistedCircuitState, cancellation);
        }
        else
        {
            await circuitPersistenceProvider.PersistCircuitAsync(
                circuit.CircuitId,
                collector.PersistedCircuitState,
                cancellation);
        }
    }

    internal async Task SaveStateToClient(CircuitHost circuit, PersistedCircuitState state, CancellationToken cancellation = default)
    {
        var (rootComponents, applicationState) = await ToProtectedStateAsync(state);
        if (!await circuit.SendPersistedStateToClient(rootComponents, applicationState, cancellation))
        {
            try
            {
                await circuitPersistenceProvider.PersistCircuitAsync(
                    circuit.CircuitId,
                    state,
                    cancellation);
            }
            catch (Exception)
            {
                // At this point, we give up as we haven't been able to save the state to the client nor the server.
                return;
            }
        }
    }

    internal async Task<(string rootComponents, string applicationState)> ToProtectedStateAsync(PersistedCircuitState state)
    {
        // Root components descriptors are already protected and serialized as JSON, we just convert the bytes to a string.
        var rootComponents = Encoding.UTF8.GetString(state.RootComponents);

        // The application state we protect in the same way we do for prerendering.
        var store = new ProtectedPrerenderComponentApplicationStore(dataProtectionProvider);
        await store.PersistStateAsync(state.ApplicationState);

        return (rootComponents, store.PersistedState);
    }

    internal PersistedCircuitState FromProtectedState(string rootComponents, string applicationState)
    {
        var rootComponentsBytes = Encoding.UTF8.GetBytes(rootComponents);
        var prerenderedState = new ProtectedPrerenderComponentApplicationStore(applicationState, dataProtectionProvider);
        var state = new PersistedCircuitState
        {
            RootComponents = rootComponentsBytes,
            ApplicationState = prerenderedState.ExistingState
        };

        return state;
    }

    internal ProtectedPrerenderComponentApplicationStore ToComponentApplicationStore(Dictionary<string, byte[]> applicationState)
    {
        return new ProtectedPrerenderComponentApplicationStore(applicationState, dataProtectionProvider);
    }

    public async Task<PersistedCircuitState> ResumeCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
    {
        return await circuitPersistenceProvider.RestoreCircuitAsync(circuitId, cancellation);
    }

    // We are going to construct a RootComponentOperationBatch but we are going to replace the descriptors from the client with the
    // descriptors that we have persisted when pausing the circuit.
    // The way pausing and resuming works is that when the client starts the resume process, it 'simulates' that an SSR has happened and
    // queues an 'Add' operation for each server-side component that is on the document.
    // That ends up calling UpdateRootComponents with the old descriptors and no application state.
    // On the server side, we replace the descriptors with the ones that we have persisted. We can't use the original descriptors because
    // those have a lifetime of ~ 5 minutes, after which we are not able to unprotect them anymore.
    internal static RootComponentOperationBatch ToRootComponentOperationBatch(
        IServerComponentDeserializer serverComponentDeserializer,
        byte[] rootComponents,
        string serializedComponentOperations)
    {
        // Deserialize the existing batch the client has sent but ignore the markers
        if (!serverComponentDeserializer.TryDeserializeRootComponentOperations(
            serializedComponentOperations,
            out var batch,
            deserializeDescriptors: false))
        {
            return null;
        }

        var persistedMarkers = TryDeserializeMarkers(rootComponents);

        if (persistedMarkers == null)
        {
            return null;
        }

        if (batch.Operations.Length != persistedMarkers.Count)
        {
            return null;
        }

        // Ensure that all operations in the batch are `Add` operations.
        for (var i = 0; i < batch.Operations.Length; i++)
        {
            var operation = batch.Operations[i];
            if (operation.Type != RootComponentOperationType.Add)
            {
                return null;
            }

            // Retrieve the marker from the persisted root components, replace it and deserialize the descriptor
            if (!persistedMarkers.TryGetValue(operation.SsrComponentId, out var marker))
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

        return batch;

        static Dictionary<int, ComponentMarker> TryDeserializeMarkers(byte[] rootComponents)
        {
            if (rootComponents == null || rootComponents.Length == 0)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<int, ComponentMarker>>(
                    rootComponents,
                    JsonSerializerOptionsProvider.Options);
            }
            catch
            {
                return null;
            }
        }
    }

    private class CircuitPersistenceManagerCollector(
        IOptions<CircuitOptions> circuitOptions,
        ServerComponentSerializer serverComponentSerializer,
        RemoteRenderer renderer)
        : IPersistentComponentStateStore
    {
        internal PersistedCircuitState PersistedCircuitState { get; private set; }

        public Task PersistRootComponents()
        {
            var persistedComponents = new Dictionary<int, ComponentMarker>();
            var components = renderer.GetOrCreateWebRootComponentManager().GetRootComponents();
            var invocation = new ServerComponentInvocationSequence();
            foreach (var (id, componentKey, (componentType, parameters)) in components)
            {
                var distributedRetention = circuitOptions.Value.PersistedCircuitInMemoryRetentionPeriod;
                var localRetention = circuitOptions.Value.PersistedCircuitInMemoryRetentionPeriod;
                var maxRetention = distributedRetention > localRetention ? distributedRetention : localRetention;

                var marker = ComponentMarker.Create(ComponentMarker.ServerMarkerType, prerendered: false, componentKey);
                serverComponentSerializer.SerializeInvocation(ref marker, invocation, componentType, parameters, maxRetention);
                persistedComponents.Add(id, marker);
            }

            PersistedCircuitState = new PersistedCircuitState
            {
                RootComponents = JsonSerializer.SerializeToUtf8Bytes(
                    persistedComponents,
                    CircuitPersistenceManagerSerializerContext.Default.DictionaryInt32ComponentMarker)
            };

            return Task.CompletedTask;
        }

        // This store only support serializing the state
        Task<IDictionary<string, byte[]>> IPersistentComponentStateStore.GetPersistedStateAsync() => throw new NotImplementedException();

        // During the persisting phase the state is captured into a Dictionary<string, byte[]>, our implementation registers
        // a callback so that it can run at the same time as the other components' state is persisted.
        // We then are called to save the persisted state, at which point, we extract the component records
        // and store them separately from the other state.
        Task IPersistentComponentStateStore.PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            PersistedCircuitState.ApplicationState = state;
            return Task.CompletedTask;
        }
    }

    [JsonSerializable(typeof(Dictionary<int, ComponentMarker>))]
    internal partial class CircuitPersistenceManagerSerializerContext : JsonSerializerContext
    {
    }
}
