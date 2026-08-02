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

public class PersistentStateValueProviderTests
{
    [Fact]
    public void CanRestoreState_ForComponentWithProperties()
    {
        // Arrange
        var state = new PersistentComponentState(
            new Dictionary<string, byte[]>(),
            [],
            []);

        var provider = new PersistentStateValueProvider(state, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
        var renderer = new TestRenderer();
        var component = new TestComponent();
        var componentStates = CreateComponentState(renderer, [(component, null)], null);
        var componentState = componentStates.First();
        var cascadingParameterInfo = CreateCascadingParameterInfo(nameof(TestComponent.State), typeof(string));

        InitializeState(state,
        new List<(ComponentState, string, string)>
        {
            (componentState, cascadingParameterInfo.PropertyName, "state")
        });

        provider.Subscribe(componentState, cascadingParameterInfo);

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
            [],
            []);

        InitializeState(state, []);

        var provider = new PersistentStateValueProvider(state, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());
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
            [],
            []);

        InitializeState(state, []);

        var provider = new PersistentStateValueProvider(state, NullLogger<PersistentStateValueProvider>.Instance, new ServiceCollection().BuildServiceProvider());

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

    private static void InitializeState(PersistentComponentState state, List<(ComponentState componentState, string propertyName, string value)> items)
    {
        var dictionary = new Dictionary<string, byte[]>();
        foreach (var item in items)
        {
            var key = PersistentStateValueProviderKeyResolver.ComputeKey(item.componentState, item.propertyName);
            dictionary[key] = JsonSerializer.SerializeToUtf8Bytes(item.value, JsonSerializerOptions.Web);
        }
        state.InitializeExistingState(dictionary, RestoreContext.InitialValue);
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

    private class ParentComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();
        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }
}
