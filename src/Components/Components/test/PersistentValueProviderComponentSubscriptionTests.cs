// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components;

public class PersistentValueProviderComponentSubscriptionTests
{
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
        var component = new TestComponent { State = "test-value" };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key, out var retrievedValue));
        Assert.Equal("test-value", retrievedValue);
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
        var component = new TestComponent { State = "test-value" };
        var parentComponent = new ParentComponent();

        var componentStates = CreateComponentState(renderer, [(component, "key1")], parentComponent);
        var componentState = componentStates.First();

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<string>(key, out var retrievedValue));
        Assert.Equal("test-value", retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_CanPersistValueTypes_IntProperty()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { IntValue = 123 };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.IntValue), typeof(int));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<int>(key, out var retrievedValue));
        Assert.Equal(123, retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_CanPersistValueTypes_NullableIntProperty()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { NullableIntValue = 456 };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.NullableIntValue), typeof(int?));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<int?>(key, out var retrievedValue));
        Assert.Equal(456, retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_CanPersistValueTypes_TupleProperty()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { TupleValue = ("test", 456) };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.TupleValue), typeof((string, int)));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<(string, int)>(key, out var retrievedValue));
        Assert.Equal(("test", 456), retrievedValue);
    }

    [Fact]
    public async Task PersistAsync_CanPersistValueTypes_NullableTupleProperty()
    {
        // Arrange
        var state = new Dictionary<string, byte[]>();
        var store = new TestStore(state);
        var persistenceManager = new ComponentStatePersistenceManager(
            NullLogger<ComponentStatePersistenceManager>.Instance,
            new ServiceCollection().BuildServiceProvider());

        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { NullableTupleValue = ("test2", 789) };
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();

        // Create the provider and subscribe the component
        var provider = new PersistentStateValueProvider(persistenceManager.State, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.NullableTupleValue), typeof((string, int)?));
        provider.Subscribe(componentState, cascadingParameterInfo);

        // Act
        await persistenceManager.PersistStateAsync(store, renderer);

        // Assert
        Assert.NotEmpty(store.State);

        // Verify the value was persisted correctly
        var newState = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        newState.InitializeExistingState(store.State, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, cascadingParameterInfo.PropertyName);
        Assert.True(newState.TryTakeFromJson<(string, int)?>(key, out var retrievedValue));
        Assert.Equal(("test2", 789), retrievedValue);
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
