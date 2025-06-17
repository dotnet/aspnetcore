// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components;
public class SupplyParameterFromPersistentComponentStateValueProviderTests
{
    [Fact]
    public void CanRestoreState_ForComponentWithProperties()
    {
        // Arrange
        var state = new PersistentComponentState(
            new Dictionary<string, byte[]>(),
            []);

        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(state);
        var renderer = new TestRenderer();
        var component = new TestComponent();
        // Update the method call to match the correct signature
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        InitializeState(state,
        new List<(ComponentState, string, string)>
        {
            (componentState, cascadingParameterInfo.PropertyName, "state")
        });

        // Act
        var result = provider.GetCurrentValue(componentState, cascadingParameterInfo);

        // Assert
        Assert.Equal("state", result);
    }

    [Fact]
    public void Subscribe_RegistersPersistenceCallback()
    {
        // Arrange
        var state = new PersistentComponentState(
            new Dictionary<string, byte[]>(),
            []);
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(state);
        var renderer = new TestRenderer();
        var component = new TestComponent();
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        // Act
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Assert
        Assert.Single(provider.Subscriptions);
    }

    [Fact]
    public void Unsubscribe_RemovesCallbackFromRegisteredCallbacks()
    {
        // Arrange
        var state = new PersistentComponentState(
            new Dictionary<string, byte[]>(),
            []);
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(state);
        var renderer = new TestRenderer();
        var component = new TestComponent();
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        provider.Unsubscribe(componentState, cascadingParameterInfo);

        // Assert
        Assert.Empty(provider.Subscriptions);
    }

    [Fact]
    public async Task PersistAsync_PersistsStateForSubscribedComponentProperties()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component = new TestComponent { State = "testValue" };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        // The key will be a hash computed from the component type and property name
        // We can verify the state was persisted by checking if any entry exists in the store
        Assert.NotEmpty(store.State);

        // To verify the actual content, we need to create a new state and restore it
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        newState.InitializeExistingState(store.State);

        // The key used for storing the property value is computed by the SupplyParameterFromPersistentComponentStateValueProvider
        var key = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key, out var retrievedValue));
        Assert.Equal("testValue", retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_UsesParentComponentType_WhenAvailable()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component = new TestComponent { State = "testValue" };
        var componentStates = CreateComponentState(renderer, [(component, null)], parentComponent);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        // The key will be a hash computed from the parent component type, component type, and property name
        Assert.NotEmpty(store.State);

        // To verify the actual content, we need to create a new state and restore it
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        newState.InitializeExistingState(store.State);

        // The key used for storing the property value is computed by the SupplyParameterFromPersistentComponentStateValueProvider
        var key = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key, out var retrievedValue));
        Assert.Equal("testValue", retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_CanPersistMultipleComponentsOfSameType_WhenParentProvidesDifferentKeys()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, 1), (component2, 2)], parentComponent);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the component
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        // The key will be a hash computed from the parent component type, component type, and property name
        Assert.NotEmpty(store.State);

        // To verify the actual content, we need to create a new state and restore it
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        newState.InitializeExistingState(store.State);

        // The key used for storing the property value is computed by the SupplyParameterFromPersistentComponentStateValueProvider
        var key1 = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState1, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key1, out var retrievedValue));
        Assert.Equal("testValue1", retrievedValue);

        var key2 = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState2, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key2, out retrievedValue));
        Assert.Equal("testValue2", retrievedValue);
    }

    public static TheoryData<object, object> ValidKeyTypesData => new TheoryData<object, object>
    {
        { true, false },
        { 'A', 'B' },
        { (sbyte)42, (sbyte)-42 },
        { (byte)240, (byte)15 },
        { (short)12345, (short)-12345 },
        { (ushort)54321, (ushort)12345 },
        { 42, -42 },
        { (uint)3000000000, (uint)1000000000 },
        { 9223372036854775807L, -9223372036854775808L },
        { (ulong)18446744073709551615UL, (ulong)1 },
        { 3.14159f, -3.14159f },
        { Math.PI, -Math.PI },
        { 123456.789m, -123456.789m },
        { new DateTime(2023, 1, 1), new DateTime(2023, 12, 31) },
        { new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.FromSeconds(0)), new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.FromSeconds(0)) },
        { "key1", "key2" },
        // Include a very long key to validate logic around growing buffers
        { new string('a', 10000), new string('b', 10000) },
        { Guid.NewGuid(), Guid.NewGuid() },
        { new DateOnly(2023, 1, 1), new DateOnly(2023, 12, 31) },
        { new TimeOnly(12, 34, 56), new TimeOnly(23, 45, 56) },
    };

    [Theory]
    [MemberData(nameof(ValidKeyTypesData))]
    public async Task PersistAsync_CanPersistMultipleComponentsOfSameType_SupportsDifferentKeyTypes(
        object componentKey1,
        object componentKey2)
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, componentKey1), (component2, componentKey2)], parentComponent);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the component
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        // The key will be a hash computed from the parent component type, component type, and property name
        Assert.NotEmpty(store.State);

        // To verify the actual content, we need to create a new state and restore it
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), []);
        newState.InitializeExistingState(store.State);

        // The key used for storing the property value is computed by the SupplyParameterFromPersistentComponentStateValueProvider
        var key1 = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState1, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key1, out var retrievedValue));
        Assert.Equal("testValue1", retrievedValue);

        var key2 = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(componentState2, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key2, out retrievedValue));
        Assert.Equal("testValue2", retrievedValue);
    }

    [Fact]
    public async Task PersistenceFails_IfMultipleComponentsOfSameType_TryToPersistDataWithoutParentComponents()
    {
        // Arrange
        var (logger, sink) = CreateTestLogger();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, null), (component2, null)], null);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the components
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.Empty(store.State);
        Assert.Contains(sink.Writes, w => w is { LogLevel: LogLevel.Error } && w.EventId == new EventId(1000, "PersistenceCallbackError"));
    }

    private static (TestLogger<ComponentStatePersistenceManager> logger, TestSink testLoggerSink) CreateTestLogger()
    {
        var testLoggerSink = new TestSink();
        var testLoggerFactory = new TestLoggerFactory(testLoggerSink, enabled: true);
        var logger = new TestLogger<ComponentStatePersistenceManager>(testLoggerFactory);
        return (logger, testLoggerSink);
    }

    [Fact]
    public async Task PersistentceFails_IfMultipleComponentsOfSameType_TryToPersistDataWithParentComponentOfSameType()
    {
        // Arrange
        var (logger, sink) = CreateTestLogger();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, null), (component2, null)], parentComponent);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the components
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.Empty(store.State);
        Assert.Contains(sink.Writes, w => w is { LogLevel: LogLevel.Error } && w.EventId == new EventId(1000, "PersistenceCallbackError"));
    }

    [Fact]
    public async Task PersistenceFails_MultipleComponentsUseTheSameKey()
    {
        // Arrange
        var (logger, sink) = CreateTestLogger();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, 1), (component2, 1)], parentComponent);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the components
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.Empty(store.State);
        Assert.Contains(sink.Writes, w => w is { LogLevel: LogLevel.Error } && w.EventId == new EventId(1000, "PersistenceCallbackError"));
    }

    public static TheoryData<object, object> InvalidKeyTypesData => new TheoryData<object, object>
    {
        { new object(), new object() },
        { new TestComponent(), new TestComponent() }
    };

    [Theory]
    [MemberData(nameof(InvalidKeyTypesData))]
    public async Task PersistenceFails_MultipleComponentsUseInvalidKeyTypes(object componentKeyType1, object componentKeyType2)
    {
        // Arrange
        var (logger, sink) = CreateTestLogger();
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            logger,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var parentComponent = new ParentComponent();
        var component1 = new TestComponent { State = "testValue1" };
        var component2 = new TestComponent { State = "testValue2" };
        var componentStates = CreateComponentState(renderer, [(component1, componentKeyType1), (component2, componentKeyType2)], parentComponent);
        var componentState1 = componentStates.First();
        var componentState2 = componentStates.Last();

        // Create the provider and subscribe the components
        var provider = new SupplyParameterFromPersistentComponentStateValueProvider(persistenceManager.State);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.Empty(store.State);
        Assert.Contains(sink.Writes, w => w is { LogLevel: LogLevel.Error } && w.EventId == new EventId(1000, "PersistenceCallbackError"));
    }

    private static void InitializeState(PersistentComponentState state, List<(ComponentState componentState, string propertyName, string value)> items)
    {
        var dictionary = new Dictionary<string, byte[]>();
        foreach (var item in items)
        {
            var key = SupplyParameterFromPersistentComponentStateValueProvider.ComputeKey(item.componentState, item.propertyName);
            dictionary[key] = JsonSerializer.SerializeToUtf8Bytes(item.value, JsonSerializerOptions.Web);
        }
        state.InitializeExistingState(dictionary);
    }

    private static CascadingParameterInfo CreateCascadingParameterInfo(string propertyName, Type propertyType)
    {
        return new CascadingParameterInfo(
            new SupplyParameterFromPersistentComponentStateAttribute(),
            propertyName,
            propertyType);
    }

    private static List<ComponentState> CreateComponentState(
        TestRenderer renderer,
        List<(TestComponent, object)> components,
        ParentComponent parentComponent = null)
    {
        var i = 1;
        var parentComponentState = parentComponent != null ? new ComponentState(renderer, i++, parentComponent, null) : null;
        var currentRenderTree = parentComponentState?.CurrentRenderTree;
        var result = new List<ComponentState>();
        foreach (var (component, key) in components)
        {
            var componentState = new ComponentState(renderer, i++, component, parentComponentState);
            if (currentRenderTree != null && key != null)
            {
                currentRenderTree.OpenComponent<TestComponent>(0);
                var frames = currentRenderTree.GetFrames();
                frames.Array[frames.Count - 1].ComponentStateField = componentState;
                if (key != null)
                {
                    currentRenderTree.SetKey(key);
                }
                currentRenderTree.CloseComponent();
            }

            result.Add(componentState);
        }

        return result;
    }

    private class TestRenderer() : Renderer(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance)
    {
        public override Dispatcher Dispatcher => Dispatcher.CreateDefault();

        protected override void HandleException(Exception exception) => throw new NotImplementedException();
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => throw new NotImplementedException();
    }

    private class TestComponent : IComponent
    {
        [SupplyParameterFromPersistentComponentState]
        public string State { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class TestStore(Dictionary<string, byte[]> initialState) : IPersistentComponentStateStore
    {
        public IDictionary<string, byte[]> State { get; set; } = initialState;

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return Task.FromResult(State);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            // We copy the data here because it's no longer available after this call completes.
            State = state.ToDictionary(k => k.Key, v => v.Value);
            return Task.CompletedTask;
        }
    }

    private class ParentComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
