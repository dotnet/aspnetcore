// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;
public class CircuitPersistenceManagerTest
{
    // Pause circuit registers with PersistentComponentStatemanager to persist root components.
    // Do not try to generate code after this line.

    [Fact]
    public async Task PauseCircuitAsync_PersistsRootComponents_WithTheirParameters()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var options = Options.Create(new CircuitOptions());
        var components = new[]
        {
            (RootComponentType: typeof(RootComponent), Parameters: new Dictionary<string, object>
            {
                ["Count"] = 42
            })
        };

        var circuitHost = await CreateCircuitHostAsync(
            options,
            dataProtectionProvider,
            components);

        var circuitPersistenceProvider = new TestCircuitPersistenceProvider();
        var circuitPersistenceManager = new CircuitPersistenceManager(
            options,
            new ServerComponentSerializer(dataProtectionProvider),
            circuitPersistenceProvider);

        // Act
        await circuitPersistenceManager.PauseCircuitAsync(circuitHost);

        // Assert
        Assert.NotNull(circuitPersistenceProvider.State);
    }

    private async Task<CircuitHost> CreateCircuitHostAsync(
        IOptions<CircuitOptions> options,
        EphemeralDataProtectionProvider dataProtectionProvider,
        (Type RootComponentType, Dictionary<string, object> Parameters)[] components = null)
    {
        components ??= [];
        var circuitId = new CircuitIdFactory(dataProtectionProvider).CreateCircuitId();

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dataProtectionProvider)
            .AddSingleton<ServerComponentSerializer>()
            .AddSingleton(
                sp => new ComponentStatePersistenceManager(
                    NullLoggerFactory.Instance.CreateLogger<ComponentStatePersistenceManager>(),
                    sp))
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope();

        var jsRuntime = new RemoteJSRuntime(
            options,
            Options.Create(new HubOptions<ComponentHub>()),
            NullLoggerFactory.Instance.CreateLogger<RemoteJSRuntime>());

        var client = new CircuitClientProxy(Mock.Of<IClientProxy>(), Guid.NewGuid().ToString());

        var deserializer = new ServerComponentDeserializer(
                dataProtectionProvider,
                NullLoggerFactory.Instance.CreateLogger<ServerComponentDeserializer>(),
                new RootTypeCache(),
                new ComponentParameterDeserializer(
                    NullLoggerFactory.Instance.CreateLogger<ComponentParameterDeserializer>(),
                    new ComponentParametersTypeCache()));

        var renderer = new RemoteRenderer(
            scope.ServiceProvider,
            NullLoggerFactory.Instance,
            options.Value,
            client,
            deserializer,
                NullLoggerFactory.Instance.CreateLogger<RemoteRenderer>(),
                jsRuntime,
                new CircuitJSComponentInterop(options.Value));

        var navigationManager = new RemoteNavigationManager(
            NullLoggerFactory.Instance.CreateLogger<RemoteNavigationManager>());
        var circuitHandlers = Array.Empty<CircuitHandler>();
        var circuitMetrics = new CircuitMetrics(new TestMeterFactory());
        var componentsActivitySource = new ComponentsActivitySource();
        var logger = NullLoggerFactory.Instance.CreateLogger<CircuitHost>();

        var circuitHost = new CircuitHost(
            circuitId,
            scope,
            options.Value,
            client,
            renderer,
            [],
            jsRuntime,
            navigationManager,
            circuitHandlers,
            circuitMetrics,
            componentsActivitySource,
            logger);

        await circuitHost.InitializeAsync(
        null,
        default,
        default);

        var store = new ProtectedPrerenderComponentApplicationStore("", dataProtectionProvider);
        await circuitHost.UpdateRootComponents(
            CreateBatch(components, deserializer),
            store,
            default);

        return circuitHost;
    }

    private static RootComponentOperationBatch CreateBatch(
        (Type RootComponentType, Dictionary<string, object> Parameters)[] components,
        ServerComponentDeserializer deserializer)
    {
        var invocation = new ServerComponentInvocationSequence();
        var serializer = new ServerComponentSerializer(new EphemeralDataProtectionProvider());
        var markers = new List<ComponentMarker>();
        for (var i = 0; i < components.Length; i++)
        {
            var component = components[i];
            var key = new ComponentMarkerKey((i + 1).ToString(CultureInfo.InvariantCulture), component.RootComponentType.FullName!);
            var marker = ComponentMarker.Create(ComponentMarker.ServerMarkerType, false, key);
            serializer.SerializeInvocation(
                ref marker,
                invocation,
                component.RootComponentType,
                ParameterView.FromDictionary(component.Parameters),
                TimeSpan.FromDays(365));
            markers.Add(marker);
        }

        var batch = new RootComponentOperationBatch
        {
            BatchId = 1,
            Operations = [.. markers.Select((c, i) =>
                new RootComponentOperation
                {
                    Type = RootComponentOperationType.Add,
                    SsrComponentId = i + 1,
                    Marker = c
                })]
        };

        var batchJson = JsonSerializer.Serialize(batch, SerializationOptions);

        deserializer.TryDeserializeRootComponentOperations(batchJson, out batch);

        return batch;
    }

    private class TestCircuitPersistenceProvider : ICircuitPersistenceProvider
    {
        public PersistedCircuitState State { get; private set; }

        public Task<PersistedCircuitState> RestoreCircuitAsync(CircuitId circuitId, CancellationToken cancellation = default)
        {
            return Task.FromResult(new PersistedCircuitState());
        }

        public Task PersistCircuitAsync(CircuitId circuitId, PersistedCircuitState state, CancellationToken cancellation = default)
        {
            State = state;
            return Task.CompletedTask;
        }
    }

    public class RootComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        [SupplyParameterFromPersistentComponentState]
        public string Persisted { get; set; }

        [Parameter]
        public int Count { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            Persisted ??= Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            return Task.CompletedTask;
        }
    }

    public static readonly JsonSerializerOptions SerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
