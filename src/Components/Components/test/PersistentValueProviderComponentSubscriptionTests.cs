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
    public void Constructor_CreatesSubscription_AndRegistersCallbacks()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "test-value" };
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        // Act
        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Assert - Constructor should complete without throwing
        Assert.NotNull(subscription);
        subscription.Dispose();
    }

    [Fact]
    public void GetOrComputeLastValue_ReturnsNull_WhenNotInitialized()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "test-value" };
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        Assert.Null(result);
        subscription.Dispose();
    }

    [Fact]
    public void GetOrComputeLastValue_RestoresFromPersistentState_OnFirstCall()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "initial-value" };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate the state with serialized data
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(TestComponent.State));
        initialState[key] = JsonSerializer.SerializeToUtf8Bytes("persisted-value", JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal("persisted-value", result);
        subscription.Dispose();
    }

    [Fact]
    public void GetOrComputeLastValue_ReturnsCurrentPropertyValue_AfterInitialization()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "current-value" };
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Initialize by calling once
        subscription.GetOrComputeLastValue();

        // Change the component's property value
        component.State = "updated-value";

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal("updated-value", result);
        subscription.Dispose();
    }

    [Fact]
    public void GetOrComputeLastValue_CanRestoreValueTypes()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { IntValue = 42 };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate the state with serialized data
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(ValueTypeTestComponent.IntValue));
        initialState[key] = JsonSerializer.SerializeToUtf8Bytes(123, JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.IntValue), typeof(int));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal(123, result);
        subscription.Dispose();
    }

    [Fact]
    public void GetOrComputeLastValue_CanRestoreNullableValueTypes()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent { NullableIntValue = 42 };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate the state with serialized data
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(ValueTypeTestComponent.NullableIntValue));
        initialState[key] = JsonSerializer.SerializeToUtf8Bytes((int?)456, JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.NullableIntValue), typeof(int?));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal(456, result);
        subscription.Dispose();
    }

    [Fact]
    public void Dispose_DisposesSubscriptions()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "test-value" };
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act & Assert - Should not throw
        subscription.Dispose();
    }

    private static CascadingParameterInfo CreateCascadingParameterInfo(string propertyName, Type propertyType)
    {
        return new CascadingParameterInfo(
            new PersistentStateAttribute(),
            propertyName,
            propertyType);
    }

    private static ComponentState CreateComponentState(
        TestRenderer renderer,
        IComponent component,
        IComponent parentComponent,
        object key)
    {
        var parentComponentState = parentComponent != null
            ? new ComponentState(renderer, 1, parentComponent, null)
            : null;
        var componentState = new ComponentState(renderer, 2, component, parentComponentState);

        if (parentComponentState != null && parentComponentState.CurrentRenderTree != null && key != null)
        {
            var currentRenderTree = parentComponentState.CurrentRenderTree;

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
            currentRenderTree.SetKey(key);
            currentRenderTree.CloseComponent();
        }

        return componentState;
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

    private class ParentComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
