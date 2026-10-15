// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;

public partial class CircuitPersistenceManagerTest
{
    // Pause circuit registers with PersistentComponentStatemanager to persist root components.
    // Do not try to generate code after this line.

    [Fact]
    public async Task PauseCircuitAsync_PersistsRootComponents_WithTheirParameters()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var deserializer = CreateDeserializer(dataProtectionProvider);

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
            deserializer,
            components);

        var circuitPersistenceProvider = new TestCircuitPersistenceProvider();
        var circuitPersistenceManager = new CircuitPersistenceManager(
            options,
            new ServerComponentSerializer(dataProtectionProvider),
            circuitPersistenceProvider,
            dataProtectionProvider);

        // Act
        await circuitPersistenceManager.PauseCircuitAsync(circuitHost);

        // Assert
        Assert.NotNull(circuitPersistenceProvider.State);
        var state = circuitPersistenceProvider.State;
        Assert.Equal(2, state.ApplicationState.Count);

        AssertRootComponents(
            deserializer,
            [
                (
                    Id: 1,
                    Key: new ComponentMarkerKey("1", typeof(RootComponent).FullName!),
                    (
                        typeof(RootComponent),
                        new Dictionary<string, object>
                        {
                            ["Count"] = 42
                        }
                    )
                )
            ],
            state.RootComponents);
    }

    [Fact]
    public async Task PauseCircuitAsync_CanPersistMultipleComponents_WithTheirParameters()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var deserializer = CreateDeserializer(dataProtectionProvider);
        var options = Options.Create(new CircuitOptions());
        var components = new[]
        {
            (RootComponentType: typeof(RootComponent), Parameters: new Dictionary<string, object>
            {
                ["Count"] = 42
            }),
            (RootComponentType: typeof(SecondRootComponent), Parameters: new Dictionary<string, object>
            {
                ["Count"] = 100
            })
        };
        var circuitHost = await CreateCircuitHostAsync(
            options,
            dataProtectionProvider,
            deserializer,
            components);
        var circuitPersistenceProvider = new TestCircuitPersistenceProvider();
        var circuitPersistenceManager = new CircuitPersistenceManager(
            options,
            new ServerComponentSerializer(dataProtectionProvider),
            circuitPersistenceProvider,
            dataProtectionProvider);
        // Act
        await circuitPersistenceManager.PauseCircuitAsync(circuitHost);
        // Assert
        Assert.NotNull(circuitPersistenceProvider.State);
        var state = circuitPersistenceProvider.State;
        Assert.Equal(3, state.ApplicationState.Count);
        AssertRootComponents(
            deserializer,
            [
                (
                    Id: 1,
                    Key: new ComponentMarkerKey("1", typeof(RootComponent).FullName!),
                    (
                        typeof(RootComponent),
                        new Dictionary<string, object>
                        {
                            ["Count"] = 42
                        }
                    )
                ),
                (
                    Id: 2,
                    Key: new ComponentMarkerKey("2", typeof(SecondRootComponent).FullName!),
                    (
                        typeof(SecondRootComponent),
                        new Dictionary<string, object>
                        {
                            ["Count"] = 100
                        }
                    )
                )
            ],
            state.RootComponents);
    }

    [Fact]
    public async Task SaveStateToClient_PersistsStateToServer_WhenSendingToClientFails()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var deserializer = CreateDeserializer(dataProtectionProvider);
        var options = Options.Create(new CircuitOptions());
        var components = new[]
        {
            (RootComponentType: typeof(RootComponent), Parameters: new Dictionary<string, object>
            {
                ["Count"] = 42
            })
        };

        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockClientProxy.Setup(c => c.InvokeCoreAsync<bool>(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate client failure

        var client = new CircuitClientProxy(mockClientProxy.Object, Guid.NewGuid().ToString());
        var circuitHost = await CreateCircuitHostAsync(
            options,
            dataProtectionProvider,
            deserializer,
            components,
            client);

        var circuitPersistenceProvider = new TestCircuitPersistenceProvider();
        var circuitPersistenceManager = new CircuitPersistenceManager(
            options,
            new ServerComponentSerializer(dataProtectionProvider),
            circuitPersistenceProvider,
            dataProtectionProvider);

        var store = new CircuitPersistenceManagerStore();

        // Create a minimal persisted state for testing
        var persistedState = new PersistedCircuitState
        {
            ApplicationState = new Dictionary<string, byte[]> { ["test"] = [1, 2, 3] },
            RootComponents = [1, 2, 3, 4]
        };

        // Act
        await circuitPersistenceManager.SaveStateToClient(
            circuitHost,
            persistedState,
            default);

        // Assert
        Assert.NotNull(circuitPersistenceProvider.State);
        Assert.Same(persistedState, circuitPersistenceProvider.State);

        // Verify that InvokeAsync was called to attempt client-side storage
        mockClientProxy.Verify(c => c.InvokeCoreAsync<bool>(
            "JS.SavePersistedState",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveStateToClient_CatchesException_WhenPersistingToServerFails()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var deserializer = CreateDeserializer(dataProtectionProvider);
        var options = Options.Create(new CircuitOptions());
        var components = new[]
        {
            (RootComponentType: typeof(RootComponent), Parameters: new Dictionary<string, object>
            {
                ["Count"] = 42
            })
        };

        var mockClientProxy = new Mock<ISingleClientProxy>();
        mockClientProxy.Setup(c => c.InvokeCoreAsync<bool>(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate client failure

        var client = new CircuitClientProxy(mockClientProxy.Object, Guid.NewGuid().ToString());
        var circuitHost = await CreateCircuitHostAsync(
            options,
            dataProtectionProvider,
            deserializer,
            components,
            client);

        // Create a circuit persistence provider that throws an exception when PersistCircuitAsync is called
        var circuitPersistenceProvider = new Mock<ICircuitPersistenceProvider>();
        circuitPersistenceProvider
            .Setup(p => p.PersistCircuitAsync(It.IsAny<CircuitId>(), It.IsAny<PersistedCircuitState>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to persist circuit"));

        var circuitPersistenceManager = new CircuitPersistenceManager(
            options,
            new ServerComponentSerializer(dataProtectionProvider),
            circuitPersistenceProvider.Object,
            dataProtectionProvider);

        var persistedState = new PersistedCircuitState
        {
            ApplicationState = new Dictionary<string, byte[]> { ["test"] = [1, 2, 3] },
            RootComponents = [1, 2, 3, 4]
        };

        // Act - This should not throw even though both client and server persistence fail
        await circuitPersistenceManager.SaveStateToClient(
            circuitHost,
            persistedState,
            default);

        // Assert
        // Verify that InvokeAsync was called to attempt client-side storage
        mockClientProxy.Verify(c => c.InvokeCoreAsync<bool>(
            "JS.SavePersistedState",
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify that PersistCircuitAsync was called when client-side storage failed
        circuitPersistenceProvider.Verify(p => p.PersistCircuitAsync(
            It.IsAny<CircuitId>(),
            It.IsAny<PersistedCircuitState>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ToRootComponentOperationBatch_WorksFor_EmptyBatch()
    {
        var deserializer = SetupMockDeserializer();

        var result = CircuitPersistenceManager.ToRootComponentOperationBatch(deserializer.Object, new Dictionary<int, WebRootComponentDescriptor>(), "ops");
        Assert.NotNull(result);
    }

    [Fact]
    public void ToRootComponentOperationBatch_Fails_IfDeserializingClientOperations_Fails()
    {
        var deserializer = SetupMockDeserializer(fail: true);
        deserializer
            .Setup(d =>
                d.TryDeserializeRootComponentOperations(
                    It.IsAny<string>(),
                    out It.Ref<RootComponentOperationBatch>.IsAny,
                    false))
            .Returns(false);

        var result = CircuitPersistenceManager.ToRootComponentOperationBatch(deserializer.Object, new Dictionary<int, WebRootComponentDescriptor>(), "ops");
        Assert.Null(result);
    }

    [Fact]
    public void ToRootComponentOperationBatch_Fails_IfDifferentNumberOfRootComponentsAndOperations()
    {
        var deserializer = SetupMockDeserializer(
            new RootComponentOperationBatch
            {
                BatchId = 1,
                Operations = [new RootComponentOperation { Type = RootComponentOperationType.Add, SsrComponentId = 1 }]
            });
        var result = CircuitPersistenceManager.ToRootComponentOperationBatch(deserializer.Object, new Dictionary<int, WebRootComponentDescriptor>(), "ops");
        Assert.Null(result);
    }

    [Fact]
    public void ToRootComponentOperationBatch_Fails_IfDescriptorForOperationNotFound()
    {
        var deserializer = SetupMockDeserializer(
            new RootComponentOperationBatch
            {
                BatchId = 1,
                Operations = [new RootComponentOperation { Type = RootComponentOperationType.Add, SsrComponentId = 1 }]
            });
        var rootComponentDescriptors = new Dictionary<int, WebRootComponentDescriptor>
        {
            [2] = new WebRootComponentDescriptor(typeof(RootComponent), new WebRootComponentParameters())
        };
        var result = CircuitPersistenceManager.ToRootComponentOperationBatch(deserializer.Object, rootComponentDescriptors, "ops");
        Assert.Null(result);
    }

    [Fact]
    public void ToRootComponentOperationBatch_WorksWhen_RootComponentsAndOperations_MatchAndCanBeDeserialized()
    {
        var deserializer = SetupMockDeserializer(
            new RootComponentOperationBatch
            {
                BatchId = 1,
                Operations = [new RootComponentOperation { Type = RootComponentOperationType.Add, SsrComponentId = 1 }]
            }, fail: false, deserializeMarker: true);
        var rootComponentDescriptors = new Dictionary<int, WebRootComponentDescriptor>
        {
            [1] = new WebRootComponentDescriptor(typeof(RootComponent), new WebRootComponentParameters())
        };
        var result = CircuitPersistenceManager.ToRootComponentOperationBatch(deserializer.Object, rootComponentDescriptors, "ops");
        Assert.NotNull(result);
    }

    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData("None")]
    [InlineData("Before")]
    [InlineData("After")]
    public void PersistStateAsync_UsesCircuitJsonTypeInfoResolvers_IndependentlyOfApplicationOptions(
        string applicationOptionsRegistration)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            bool.FalseString);

        using var remoteHandle = RemoteExecutor.Invoke(static async applicationOptionsRegistration =>
        {
            Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

            var unrelatedOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper,
                WriteIndented = true,
            };
            var services = new ServiceCollection();
            var sink = new TestSink();
            using var loggerFactory = new TestLoggerFactory(sink, true);
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(value => value.ApplicationName).Returns(nameof(CircuitPersistenceManagerTest));
            environment.SetupGet(value => value.WebRootFileProvider).Returns(new NullFileProvider());
            services.AddSingleton(environment.Object);
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            if (applicationOptionsRegistration == "Before")
            {
                services.AddScoped(_ => unrelatedOptions);
            }
            var builder = services.AddRazorComponents()
                .RegisterPersistentService<TestPersistentService>(RenderMode.InteractiveServer);
#pragma warning disable ASPNETCORE9004 // The framework implements this experimental extension point.
            builder.AddInteractiveServerComponents(options =>
                options.JsonTypeInfoResolvers.Add(TestJsonSerializerContext.Default));
#pragma warning restore ASPNETCORE9004
            services.AddScoped<TestPersistentService>();
            if (applicationOptionsRegistration == "After")
            {
                services.AddScoped(_ => unrelatedOptions);
            }

            await using var serviceProvider = services.BuildServiceProvider();
            var store = new TestStore(new Dictionary<string, byte[]>());

            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<TestPersistentService>().Customer = new Customer
                {
                    Name = "John Doe",
                };
                var persistenceManager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
                persistenceManager.SetPlatformRenderMode(RenderMode.InteractiveServer);
                await persistenceManager.RestoreStateAsync(
                    new TestStore(new Dictionary<string, byte[]>()),
                    RestoreContext.InitialValue);
                await persistenceManager.PersistStateAsync(store, new PersistenceTestRenderer());

                Assert.All(sink.Writes, write => Assert.Null(write.Exception));
                Assert.Contains("""{"name":"John Doe"}""", store.State.Values.Select(data => Encoding.UTF8.GetString(data)));
                if (applicationOptionsRegistration == "None")
                {
                    Assert.Null(scope.ServiceProvider.GetService<JsonSerializerOptions>());
                }
                else
                {
                    Assert.Same(unrelatedOptions, scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>());
                }
            }

            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var persistenceManager = scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>();
                persistenceManager.SetPlatformRenderMode(RenderMode.InteractiveServer);
                await persistenceManager.RestoreStateAsync(store, RestoreContext.InitialValue);

                var customer = scope.ServiceProvider.GetRequiredService<TestPersistentService>().Customer;
                Assert.NotNull(customer);
                Assert.Equal("John Doe", customer.Name);
            }

            Assert.False(unrelatedOptions.IsReadOnly);
            Assert.Empty(unrelatedOptions.TypeInfoResolverChain);
        }, applicationOptionsRegistration, options);
    }

    private void AssertRootComponents(
        ServerComponentDeserializer deserializer,
        (int Id, ComponentMarkerKey Key, (Type ComponentType, Dictionary<string, object> Parameters))[] expected, byte[] rootComponents)
    {
        var actual = JsonSerializer.Deserialize<Dictionary<int, ComponentMarker>>(rootComponents, SerializerOptions);
        Assert.NotNull(actual);
        Assert.Equal(expected.Length, actual.Count);
        foreach (var (id, key, (componentType, parameters)) in expected)
        {
            Assert.True(actual.TryGetValue(id, out var marker), $"Expected marker with ID {id} not found.");
            Assert.Equal(key.LocationHash, marker.Key.Value.LocationHash);
            Assert.Equal(key.FormattedComponentKey, marker.Key.Value.FormattedComponentKey);
            Assert.True(deserializer.TryDeserializeWebRootComponentDescriptor(marker, out var descriptor), $"Failed to deserialize marker with ID {id}.");
            Assert.NotNull(descriptor);
            Assert.Equal(componentType, descriptor.ComponentType);
            var actualParameters = descriptor.Parameters.Parameters.ToDictionary();
            Assert.NotNull(actualParameters);
            Assert.Equal(parameters.Count, actualParameters.Count);
            foreach (var (paramKey, paramValue) in parameters)
            {
                Assert.True(actualParameters.TryGetValue(paramKey, out var actualValue), $"Expected parameter '{paramKey}' not found.");
                Assert.Equal(paramValue, actualValue);
            }
        }
    }

    private async Task<CircuitHost> CreateCircuitHostAsync(
        IOptions<CircuitOptions> options,
        EphemeralDataProtectionProvider dataProtectionProvider,
        ServerComponentDeserializer deserializer,
        (Type RootComponentType, Dictionary<string, object> Parameters)[] components = null,
        CircuitClientProxy client = null)
    {
        components ??= [];
        var circuitId = new CircuitIdFactory(dataProtectionProvider).CreateCircuitId();

        var jsRuntime = new RemoteJSRuntime(
            options,
            Options.Create(new HubOptions<ComponentHub>()),
            NullLoggerFactory.Instance.CreateLogger<RemoteJSRuntime>());

        var serviceProvider = new ServiceCollection()
            .AddSingleton(dataProtectionProvider)
            .AddSingleton<ServerComponentSerializer>()
            .AddSupplyValueFromPersistentComponentStateProvider()
            .AddFakeLogging()
            .AddSingleton(
                sp => new ComponentStatePersistenceManager(
                    NullLoggerFactory.Instance.CreateLogger<ComponentStatePersistenceManager>(),
                    sp))
            .AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State)
            .AddSingleton<IJSRuntime>(jsRuntime)
            .BuildServiceProvider();

        var scope = serviceProvider.CreateAsyncScope();

        client ??= new CircuitClientProxy(Mock.Of<ISingleClientProxy>(), Guid.NewGuid().ToString());

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
        var componentsActivitySource = new CircuitActivitySource();
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

        var store = new ProtectedPrerenderComponentApplicationStore(dataProtectionProvider);
        await circuitHost.UpdateRootComponents(
            CreateBatch(components, deserializer, dataProtectionProvider),
            store,
            false,
            default);

        return circuitHost;
    }

    private static ServerComponentDeserializer CreateDeserializer(EphemeralDataProtectionProvider dataProtectionProvider) => new ServerComponentDeserializer(
                    dataProtectionProvider,
                    NullLoggerFactory.Instance.CreateLogger<ServerComponentDeserializer>(),
                    new RootTypeCache(),
                    new ComponentParameterDeserializer(
                        NullLoggerFactory.Instance.CreateLogger<ComponentParameterDeserializer>(),
                        new ComponentParametersTypeCache()));

    private static Mock<IServerComponentDeserializer> SetupMockDeserializer(
    RootComponentOperationBatch batchResult = default,
    bool fail = false,
    bool deserializeMarker = false)
    {
        var deserializer = new Mock<IServerComponentDeserializer>();
        batchResult = fail ?
            default :
            batchResult == default ?
                new RootComponentOperationBatch
                {
                    Operations = [],
                    BatchId = 1
                } :
                batchResult;

        deserializer
            .Setup(d =>
                d.TryDeserializeRootComponentOperations(
                    It.IsAny<string>(),
                    out It.Ref<RootComponentOperationBatch>.IsAny,
                    false))
            .Callback((string serializedOps, out RootComponentOperationBatch batch, bool value) =>
            {
                batch = batchResult;
            })
            .Returns(!fail);

        if (deserializeMarker)
        {
            deserializer.Setup(deserializer =>
                deserializer.TryDeserializeWebRootComponentDescriptor(
                    It.IsAny<ComponentMarker>(),
                    out It.Ref<WebRootComponentDescriptor>.IsAny))
                .Callback((ComponentMarker marker, out WebRootComponentDescriptor descriptor) =>
                {
                    descriptor = new WebRootComponentDescriptor(typeof(RootComponent), new WebRootComponentParameters());
                })
                .Returns(true);
        }
        return deserializer;
    }

    private static RootComponentOperationBatch CreateBatch(
        (Type RootComponentType, Dictionary<string, object> Parameters)[] components,
        ServerComponentDeserializer deserializer,
        EphemeralDataProtectionProvider dataProtectionProvider)
    {
        var invocation = new ServerComponentInvocationSequence();
        var serializer = new ServerComponentSerializer(dataProtectionProvider);
        var markers = new List<ComponentMarker>();
        for (var i = 0; i < components.Length; i++)
        {
            var (rootComponentType, parameters) = components[i];
            var key = new ComponentMarkerKey((i + 1).ToString(CultureInfo.InvariantCulture), rootComponentType.FullName!);
            var marker = ComponentMarker.Create(ComponentMarker.ServerMarkerType, false, key);
            serializer.SerializeInvocation(
                ref marker,
                invocation,
                rootComponentType,
                ParameterView.FromDictionary(parameters),
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

        var batchJson = JsonSerializer.Serialize(batch, ServerComponentSerializationSettings.JsonSerializationOptions);

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

    private sealed class TestStore(IDictionary<string, byte[]> state) : IPersistentComponentStateStore
    {
        public IDictionary<string, byte[]> State { get; private set; } = state;

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(State);

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            State = state.ToDictionary();
            return Task.CompletedTask;
        }
    }

    private sealed class PersistenceTestRenderer()
        : Renderer(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance)
    {
        private readonly Dispatcher _dispatcher = Dispatcher.CreateDefault();

        public override Dispatcher Dispatcher => _dispatcher;

        protected override void HandleException(Exception exception) => throw new NotImplementedException();

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }

    private sealed class TestPersistentService
    {
        [PersistentState]
        public Customer Customer { get; set; }
    }

    private sealed class Customer
    {
        public string Name { get; set; }
    }

    [JsonSerializable(typeof(Customer))]
    private sealed partial class TestJsonSerializerContext : JsonSerializerContext
    {
    }

    public class RootComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        [PersistentState]
        public string Persisted { get; set; }

        [Parameter]
        public int Count { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            Persisted ??= Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            _renderHandle.Render(rtb =>
            {
                rtb.OpenElement(0, "div");
                rtb.AddContent(1, $"Persisted: {Persisted}, Count: {Count}");
                rtb.CloseElement();
            });

            return Task.CompletedTask;
        }
    }

    public class SecondRootComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        [PersistentState]
        public string Persisted { get; set; }

        [Parameter]
        public int Count { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            Persisted ??= Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            _renderHandle.Render(rtb =>
            {
                rtb.OpenElement(0, "div");
                rtb.AddContent(1, $"Persisted: {Persisted}, Count: {Count}");
                rtb.CloseElement();
            });

            return Task.CompletedTask;
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private class CircuitPersistenceManagerStore : IPersistentComponentStateStore
    {
        internal PersistedCircuitState PersistedCircuitState { get; private set; }

        Task<IDictionary<string, byte[]>> IPersistentComponentStateStore.GetPersistedStateAsync() =>
            throw new NotImplementedException();

        Task IPersistentComponentStateStore.PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            PersistedCircuitState = new PersistedCircuitState
            {
                ApplicationState = new Dictionary<string, byte[]>(state),
                RootComponents = null
            };

            return Task.CompletedTask;
        }
    }
}
