// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading.Tasks;
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

    [Fact]
    public void GetOrComputeLastValue_ReturnsNull_WhenSkipInitialValueAndInitialContext()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new TestComponent { State = "initial-value" };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate the state with serialized data that should be skipped
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(TestComponent.State));
        initialState[key] = JsonSerializer.SerializeToUtf8Bytes("persisted-value", JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.InitialValue);

        var cascadingParameterInfo = CreateCascadingParameterInfoWithBehavior(
            nameof(TestComponent.State),
            typeof(string),
            RestoreBehavior.SkipInitialValue);

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
    public async Task GetOrComputeLastValue_FollowsCorrectValueTransitionSequence()
    {
        // Arrange
        var appState = new Dictionary<string, byte[]>();
        var manager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        var state = manager.State;
        var serviceProvider = PersistentStateProviderServiceCollectionExtensions.AddSupplyValueFromPersistentComponentStateProvider(new ServiceCollection())
            .AddSingleton(manager)
            .AddSingleton(manager.State)
            .AddFakeLogging()
            .BuildServiceProvider();
        var renderer = new TestRenderer(serviceProvider);
        var provider = (PersistentStateValueProvider)renderer.ServiceProviderCascadingValueSuppliers.Single();
        var component = new TestComponent { State = "initial-property-value" };
        var componentId = renderer.AssignRootComponentId(component);
        var componentState = renderer.GetComponentState(component);

        // Pre-populate the state with serialized data
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(TestComponent.State));
        appState[key] = JsonSerializer.SerializeToUtf8Bytes("first-persisted-value", JsonSerializerOptions.Web);
        await manager.RestoreStateAsync(new TestStore(appState), RestoreContext.InitialValue);

        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootComponentAsync(componentId, ParameterView.Empty));
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        // Act & Assert - First call: Returns restored value from state
        Assert.Equal("first-persisted-value", provider.GetCurrentValue(componentState, cascadingParameterInfo));

        // Change the component's property value
        component.State = "updated-property-value";

        // Second call: Returns the component's property value
        var result2 = provider.GetCurrentValue(componentState, cascadingParameterInfo);
        Assert.Equal("updated-property-value", result2);

        appState.Clear();
        var newState = new Dictionary<string, byte[]>
        {
            [key] = JsonSerializer.SerializeToUtf8Bytes("second-restored-value", JsonSerializerOptions.Web)
        };
        // Simulate invoking the callback with a value update.
        await renderer.Dispatcher.InvokeAsync(() => manager.RestoreStateAsync(new TestStore(newState), RestoreContext.ValueUpdate));
        Assert.Equal("second-restored-value", provider.GetCurrentValue(componentState, cascadingParameterInfo));

        component.State = "another-updated-value";
        // Other calls: Returns the updated value from state
        Assert.Equal("another-updated-value", provider.GetCurrentValue(componentState, cascadingParameterInfo));
        component.State = "final-updated-value";
        Assert.Equal("final-updated-value", provider.GetCurrentValue(componentState, cascadingParameterInfo));
        Assert.Equal("final-updated-value", provider.GetCurrentValue(componentState, cascadingParameterInfo));
    }

    [Fact]
    public void GetOrComputeLastValue_UsesCustomSerializer_ForRestoration()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new CustomSerializerTestComponent { CustomValue = new CustomData { Value = "initial" } };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate the state with custom serialized data
        var key = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(CustomSerializerTestComponent.CustomValue));
        var customSerializer = new TestCustomDataSerializer();
        var testData = new CustomData { Value = "restored-custom" };
        var writer = new ArrayBufferWriter<byte>();
        customSerializer.Persist(testData, writer);
        initialState[key] = writer.WrittenSpan.ToArray();
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var cascadingParameterInfo = CreateCascadingParameterInfo(
            nameof(CustomSerializerTestComponent.CustomValue),
            typeof(CustomData));

        var serviceProvider = new ServiceCollection()
            .AddSingleton<PersistentComponentStateSerializer<CustomData>, TestCustomDataSerializer>()
            .BuildServiceProvider();
        var logger = NullLogger.Instance;

        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        // Act
        var result = subscription.GetOrComputeLastValue();

        // Assert
        var customDataResult = Assert.IsType<CustomData>(result);
        Assert.Equal("restored-custom", customDataResult.Value);
        subscription.Dispose();
    }

    [Fact]
    public void CanPersistAndRestore_MultipleProperties_OnSameComponent()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new MultiplePropertiesComponent
        {
            StringValue = "initial-string",
            IntValue = 42,
            BoolValue = true
        };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate state for all properties
        var stringKey = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(MultiplePropertiesComponent.StringValue));
        var intKey = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(MultiplePropertiesComponent.IntValue));
        var boolKey = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(MultiplePropertiesComponent.BoolValue));

        initialState[stringKey] = JsonSerializer.SerializeToUtf8Bytes("restored-string", JsonSerializerOptions.Web);
        initialState[intKey] = JsonSerializer.SerializeToUtf8Bytes(123, JsonSerializerOptions.Web);
        initialState[boolKey] = JsonSerializer.SerializeToUtf8Bytes(false, JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        // Create subscriptions for each property
        var stringSubscription = new PersistentValueProviderComponentSubscription(
            state, componentState,
            CreateCascadingParameterInfo(nameof(MultiplePropertiesComponent.StringValue), typeof(string)),
            serviceProvider, logger);

        var intSubscription = new PersistentValueProviderComponentSubscription(
            state, componentState,
            CreateCascadingParameterInfo(nameof(MultiplePropertiesComponent.IntValue), typeof(int)),
            serviceProvider, logger);

        var boolSubscription = new PersistentValueProviderComponentSubscription(
            state, componentState,
            CreateCascadingParameterInfo(nameof(MultiplePropertiesComponent.BoolValue), typeof(bool)),
            serviceProvider, logger);

        // Act
        var stringResult = stringSubscription.GetOrComputeLastValue();
        var intResult = intSubscription.GetOrComputeLastValue();
        var boolResult = boolSubscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal("restored-string", stringResult);
        Assert.Equal(123, intResult);
        Assert.Equal(false, boolResult);

        // Cleanup
        stringSubscription.Dispose();
        intSubscription.Dispose();
        boolSubscription.Dispose();
    }

    [Fact]
    public void CanPersistAndRestore_DifferentPropertyTypes_OnSameComponent()
    {
        // Arrange
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer();
        var component = new ValueTypeTestComponent
        {
            IntValue = 42,
            NullableIntValue = 100
        };
        var componentState = CreateComponentState(renderer, component, null, null);

        // Pre-populate state for different property types
        var intKey = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(ValueTypeTestComponent.IntValue));
        var nullableIntKey = PersistentStateValueProviderKeyResolver.ComputeKey(componentState, nameof(ValueTypeTestComponent.NullableIntValue));

        initialState[intKey] = JsonSerializer.SerializeToUtf8Bytes(999, JsonSerializerOptions.Web);
        initialState[nullableIntKey] = JsonSerializer.SerializeToUtf8Bytes((int?)777, JsonSerializerOptions.Web);
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        // Create subscriptions for different property types
        var intSubscription = new PersistentValueProviderComponentSubscription(
            state, componentState,
            CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.IntValue), typeof(int)),
            serviceProvider, logger);

        var nullableIntSubscription = new PersistentValueProviderComponentSubscription(
            state, componentState,
            CreateCascadingParameterInfo(nameof(ValueTypeTestComponent.NullableIntValue), typeof(int?)),
            serviceProvider, logger);

        // Act
        var intResult = intSubscription.GetOrComputeLastValue();
        var nullableIntResult = nullableIntSubscription.GetOrComputeLastValue();

        // Assert
        Assert.Equal(999, intResult);
        Assert.Equal(777, nullableIntResult);

        // Cleanup
        intSubscription.Dispose();
        nullableIntSubscription.Dispose();
    }

    private static CascadingParameterInfo CreateCascadingParameterInfo(string propertyName, Type propertyType)
    {
        return new CascadingParameterInfo(
            new PersistentStateAttribute(),
            propertyName,
            propertyType);
    }

    private static CascadingParameterInfo CreateCascadingParameterInfoWithBehavior(
        string propertyName,
        Type propertyType,
        RestoreBehavior restoreBehavior,
        bool allowUpdates = false)
    {
        return new CascadingParameterInfo(
            new PersistentStateAttribute
            {
                RestoreBehavior = restoreBehavior,
                AllowUpdates = allowUpdates
            },
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

    private class TestRenderer(IServiceProvider serviceProvider) : Renderer(serviceProvider, NullLoggerFactory.Instance)
    {
        public TestRenderer() : this(new ServiceCollection().BuildServiceProvider()) { }

        public override Dispatcher Dispatcher => new TestDispatcher();

        protected override void HandleException(Exception exception) => ExceptionDispatchInfo.Capture(exception);
        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => throw new NotImplementedException();
    }

    private class TestDispatcher : Dispatcher
    {
        public override bool CheckAccess() => true;
        public override Task InvokeAsync(Action workItem)
        {
            workItem();
            return Task.CompletedTask;
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            return workItem();
        }
        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            return Task.FromResult(workItem());
        }
        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            return workItem();
        }
    }

    private class TestComponent : IComponent
    {
        [PersistentState(AllowUpdates = true)]
        public string State { get; set; }

        public void Attach(RenderHandle renderHandle)
        {            
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }
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

    private class MultiplePropertiesComponent : IComponent
    {
        [PersistentState]
        public string StringValue { get; set; }

        [PersistentState]
        public int IntValue { get; set; }

        [PersistentState]
        public bool BoolValue { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class CustomSerializerTestComponent : IComponent
    {
        [PersistentState]
        public CustomData CustomValue { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class CustomData
    {
        public string Value { get; set; }
    }

    private class TestCustomDataSerializer : PersistentComponentStateSerializer<CustomData>
    {
        public override void Persist(CustomData value, IBufferWriter<byte> writer)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes($"CUSTOM:{value.Value}");
            writer.Write(json);
        }

        public override CustomData Restore(ReadOnlySequence<byte> data)
        {
            var json = JsonSerializer.Deserialize<string>(data.ToArray());
            var value = json.StartsWith("CUSTOM:", StringComparison.Ordinal) ? json.Substring(7) : json;
            return new CustomData { Value = value };
        }
    }

    private class ParentComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class TestStore(IDictionary<string, byte[]> state) : IPersistentComponentStateStore
    {
        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(state);
        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => throw new NotImplementedException();
    }

    private class ComponentWithPrivateProperty : IComponent
    {
        [PersistentState]
        private string PrivateValue { get; set; } = "initial";

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private class ComponentWithPrivateGetter : IComponent
    {
        [PersistentState]
        public string PropertyWithPrivateGetter { private get; set; } = "initial";

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    [Fact]
    public void Constructor_ThrowsClearException_ForPrivateProperty()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new ComponentWithPrivateProperty();
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo("PrivateValue", typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PersistentValueProviderComponentSubscription(
                state, componentState, cascadingParameterInfo, serviceProvider, logger));

        // Should throw a clear error about needing a public property with public getter
        Assert.Contains("A public property", exception.Message);
        Assert.Contains("with a public getter wasn't found", exception.Message);
    }

    [Fact]
    public void Constructor_ThrowsClearException_ForPrivateGetter()
    {
        // Arrange
        var state = new PersistentComponentState(new Dictionary<string, byte[]>(), [], []);
        state.InitializeExistingState(new Dictionary<string, byte[]>(), RestoreContext.InitialValue);
        var renderer = new TestRenderer();
        var component = new ComponentWithPrivateGetter();
        var componentState = CreateComponentState(renderer, component, null, null);
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(ComponentWithPrivateGetter.PropertyWithPrivateGetter), typeof(string));
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var logger = NullLogger.Instance;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new PersistentValueProviderComponentSubscription(
                state, componentState, cascadingParameterInfo, serviceProvider, logger));

        // Should throw a clear error about needing a public property with public getter
        Assert.Contains("A public property", exception.Message);
        Assert.Contains("with a public getter wasn't found", exception.Message);
    }

    [Fact]
    public void Constructor_WorksCorrectly_ForPublicProperty()
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

        // Act & Assert - Should not throw
        var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, cascadingParameterInfo, serviceProvider, logger);

        Assert.NotNull(subscription);
        subscription.Dispose();
    }
}
