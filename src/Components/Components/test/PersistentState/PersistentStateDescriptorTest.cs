// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components.Infrastructure;

public class PersistentStateDescriptorTest
{
    [Fact]
    public void CustomSerializerComesFromTheDescriptorWhenTheComponentIsDescribed()
    {
        var serializer = new MarkerSerializer();
        var services = BuildServices(new StubResolver(Describe(
            typeof(DescribedStateComponent),
            services => serializer)));

        var value = Restore(services, new CustomState { Value = "persisted" });

        Assert.Equal("marker:persisted", value.Value);
        Assert.True(serializer.Restored);
    }

    [Fact]
    public void NoCustomSerializerIsUsedWhenTheDescriptorDoesNotNameOne()
    {
        var services = BuildServices(
            new StubResolver(Describe(typeof(DescribedStateComponent), getStateSerializer: null)),
            registerSerializer: true);

        var value = Restore(services, new CustomState { Value = "persisted" }, useCustomSerializerForSetup: false);

        // The descriptor describes the component completely, so the container is not consulted.
        Assert.Equal("persisted", value.Value);
    }

    [Fact]
    public void ContainerRegisteredSerializerIsStillUsedWhenNoMetadataIsRegistered()
    {
        var services = new ServiceCollection()
            .AddSingleton<PersistentComponentStateSerializer<CustomState>, MarkerSerializer>()
            .BuildServiceProvider();

        var value = Restore(services, new CustomState { Value = "persisted" });

        Assert.Equal("marker:persisted", value.Value);
    }

    private static CustomState Restore(
        IServiceProvider services,
        CustomState persisted,
        bool useCustomSerializerForSetup = true)
    {
        var initialState = new Dictionary<string, byte[]>();
        var state = new PersistentComponentState(initialState, [], []);
        var renderer = new TestRenderer(services);
        var component = new DescribedStateComponent();
        var componentState = new ComponentState(renderer, 2, component, null);

        var key = PersistentStateValueProviderKeyResolver.ComputeKey(
            componentState,
            nameof(DescribedStateComponent.CustomValue));

        var writer = new ArrayBufferWriter<byte>();
        if (useCustomSerializerForSetup)
        {
            new MarkerSerializer().Persist(persisted, writer);
        }
        else
        {
            writer.Write(JsonSerializer.SerializeToUtf8Bytes(persisted, JsonSerializerOptions.Web));
        }

        initialState[key] = writer.WrittenSpan.ToArray();
        state.InitializeExistingState(initialState, RestoreContext.LastSnapshot);

        var parameterInfo = new CascadingParameterInfo(
            new PersistentStateAttribute(),
            nameof(DescribedStateComponent.CustomValue),
            typeof(CustomState));

        using var subscription = new PersistentValueProviderComponentSubscription(
            state, componentState, parameterInfo, services, NullLogger.Instance);

        return Assert.IsType<CustomState>(subscription.GetOrComputeLastValue());
    }

    private static IServiceProvider BuildServices(IComponentMetadataResolver resolver, bool registerSerializer = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(resolver);
        if (registerSerializer)
        {
            services.AddSingleton<PersistentComponentStateSerializer<CustomState>, MarkerSerializer>();
        }

        return services.BuildServiceProvider();
    }

    private static ComponentDescriptor Describe(Type type, Func<IServiceProvider, object?> getStateSerializer)
        => new()
        {
            Type = type,
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(DescribedStateComponent.CustomValue),
                    ParameterType = typeof(CustomState),
                    Attribute = new PersistentStateAttribute(),
                    SetValue = static (target, value) => ((DescribedStateComponent)target).CustomValue = (CustomState)value,
                    GetValue = static target => ((DescribedStateComponent)target).CustomValue,
                    GetStateSerializer = getStateSerializer,
                },
            ],
        };

    private sealed class StubResolver(params ComponentDescriptor[] descriptors) : IComponentMetadataResolver
    {
        private readonly Dictionary<Type, ComponentDescriptor> _descriptors = descriptors.ToDictionary(d => d.Type);

        public IReadOnlyList<ComponentDescriptor> Components => [.. _descriptors.Values];

        public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor descriptor)
            => _descriptors.TryGetValue(type, out descriptor!);
    }

    private sealed class DescribedStateComponent : IComponent
    {
        [PersistentState]
        public CustomState CustomValue { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private sealed class CustomState
    {
        public string Value { get; set; }
    }

    private sealed class MarkerSerializer : PersistentComponentStateSerializer<CustomState>
    {
        public bool Restored { get; private set; }

        public override void Persist(CustomState value, IBufferWriter<byte> writer)
            => writer.Write(JsonSerializer.SerializeToUtf8Bytes($"marker:{value.Value}"));

        public override CustomState Restore(ReadOnlySequence<byte> data)
        {
            Restored = true;
            return new CustomState { Value = JsonSerializer.Deserialize<string>(data.ToArray()) };
        }
    }
}
