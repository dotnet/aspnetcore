// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Components;

public class PersistentStateValueProviderKeyResolverTests
{
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

    public static TheoryData<object, object> InvalidKeyTypesData => new TheoryData<object, object>
    {
        { new object(), new object() },
        { new TestComponent(), new TestComponent() }
    };

    [Fact]
    public async Task PersistAsync_CanPersistMultipleComponentsOfSameType_WhenParentProvidesDifferentKeys()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        persistenceManager.State.InitializeExistingState(state, RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var parentComponent = new ParentComponent();

        var componentStates = CreateComponentState(renderer, [(component1, "key1"), (component2, "key2")], parentComponent);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Check that keys are different for different component instances with different keys
        var key1 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState1, cascadingParameterInfo.PropertyName);
        var key2 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState2, cascadingParameterInfo.PropertyName);

        Assert.NotEqual(key1, key2);

        // Verify both states were persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        Assert.True(newState.TryTakeFromJson<string>(key1, out var retrievedValue1));
        Assert.Equal("state1", retrievedValue1);

        Assert.True(newState.TryTakeFromJson<string>(key2, out var retrievedValue2));
        Assert.Equal("state2", retrievedValue2);
    }

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

        await persistenceManager.RestoreStateAsync(new TestStore([]), RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var parentComponent = new ParentComponent();

        var componentStates = CreateComponentState(renderer, [(component1, componentKey1), (component2, componentKey2)], parentComponent);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Check that keys are different for different component instances with different keys
        var key1 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState1, cascadingParameterInfo.PropertyName);
        var key2 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState2, cascadingParameterInfo.PropertyName);

        Assert.NotEqual(key1, key2);

        // Verify both states were persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        Assert.True(newState.TryTakeFromJson<string>(key1, out var retrievedValue1));
        Assert.Equal("state1", retrievedValue1);

        Assert.True(newState.TryTakeFromJson<string>(key2, out var retrievedValue2));
        Assert.Equal("state2", retrievedValue2);
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

        persistenceManager.State.InitializeExistingState(state, RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var componentStates = CreateComponentState(renderer, [(component1, null), (component2, null)], null);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Check that keys are the same for multiple component instances without keys
        var key1 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState1, cascadingParameterInfo.PropertyName);
        var key2 = PersistentStateValueProviderKeyResolver.ComputeKey(componentState2, cascadingParameterInfo.PropertyName);

        Assert.Equal(key1, key2);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        var messages = sink.Writes.Where(x => x.LogLevel >= LogLevel.Warning).ToList();
        Assert.Single(messages);
    }

    private static (ILogger<ComponentStatePersistenceManager> logger, TestSink testLoggerSink) CreateTestLogger()
    {
        var testLoggerSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testLoggerSink, enabled: true);
        var logger = loggerFactory.CreateLogger<ComponentStatePersistenceManager>();
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

        persistenceManager.State.InitializeExistingState(state, RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var parentComponent = new TestComponent();

        var componentStates = CreateComponentState(renderer, [(component1, null), (component2, null)], parentComponent);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        var messages = sink.Writes.Where(x => x.LogLevel >= LogLevel.Warning).ToList();
        Assert.Single(messages);
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

        persistenceManager.State.InitializeExistingState(state, RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var parentComponent = new ParentComponent();

        var componentStates = CreateComponentState(renderer, [(component1, "key1"), (component2, "key1")], parentComponent);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        var messages = sink.Writes.Where(x => x.LogLevel >= LogLevel.Warning).ToList();
        Assert.Single(messages);
    }

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

        persistenceManager.State.InitializeExistingState(state, RestoreContext.InitialValue);

        var renderer = new TestRenderer();
        var component1 = new TestComponent { State = "state1" };
        var component2 = new TestComponent { State = "state2" };

        var parentComponent = new ParentComponent();

        var componentStates = CreateComponentState(renderer, [(component1, componentKeyType1), (component2, componentKeyType2)], parentComponent);
        var componentState1 = componentStates[0];
        var componentState2 = componentStates[1];

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState1, cascadingParameterInfo);
        provider.Subscribe(componentState2, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        var messages = sink.Writes.Where(x => x.LogLevel >= LogLevel.Warning).ToList();
        Assert.Single(messages);
    }

    private static CascadingParameterInfo CreateCascadingParameterInfo(string propertyName, Type propertyType)
    {
        return new CascadingParameterInfo(
            new PersistentStateAttribute(),
            propertyName,
            propertyType);
    }

    private static List<ComponentState> CreateComponentState(
        TestRenderer renderer,
        List<(IComponent, object)> components,
        IComponent parentComponent = null)
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
                // Open component based on the actual component type
                if (component is TestComponent)
                {
                    currentRenderTree.OpenComponent<TestComponent>(0);
                }
                else if (component is ValueTypeTestComponent)
                {
                    currentRenderTree.OpenComponent<ValueTypeTestComponent>(0);
                }
                else
                {
                    currentRenderTree.OpenComponent<IComponent>(0);
                }

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
        [PersistentState]
        public string State { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class ValueTypeTestComponent : IComponent
    {
        [PersistentState]
        public int IntValue { get; set; }

        [PersistentState]
        public int? NullableIntValue { get; set; }

        [PersistentState]
        public (string, int) TupleValue { get; set; }

        [PersistentState]
        public (string, int)? NullableTupleValue { get; set; }

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
