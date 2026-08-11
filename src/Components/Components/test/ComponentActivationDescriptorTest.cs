// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components;

public class ComponentActivationDescriptorTest
{
    [Fact]
    public void DefaultComponentActivator_UsesTheDescriptorFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IComponentMetadataResolver>(
            new StubResolver(DescribedComponent.DescriptorWithFactory));
        var serviceProvider = services.BuildServiceProvider();
        var activator = new DefaultComponentActivator(serviceProvider);

        var instance = Assert.IsType<DescribedComponent>(activator.CreateInstance(typeof(DescribedComponent)));

        Assert.True(instance.CreatedByDescriptor);
    }

    [Fact]
    public void DefaultComponentActivator_FallsBackWhenTheDescriptorHasNoFactory()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IComponentMetadataResolver>(
            new StubResolver(DescribedComponent.DescriptorWithoutFactory));
        var serviceProvider = services.BuildServiceProvider();
        var activator = new DefaultComponentActivator(serviceProvider);

        var instance = Assert.IsType<DescribedComponent>(activator.CreateInstance(typeof(DescribedComponent)));

        Assert.False(instance.CreatedByDescriptor);
    }

    [Fact]
    public void DefaultComponentActivator_RejectsNonComponentsBeforeConsultingTheDescriptor()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var activator = new DefaultComponentActivator(serviceProvider);

        var ex = Assert.Throws<ArgumentException>(() => activator.CreateInstance(typeof(List<string>)));

        Assert.StartsWith($"The type {typeof(List<string>).FullName} does not implement {nameof(IComponent)}.", ex.Message);
    }

    [Fact]
    public void DefaultComponentPropertyActivator_InjectsThroughTheDescriptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SomeService());
        var serviceProvider = services.BuildServiceProvider();
        var resolver = CreateTypeInfoResolver(InjectableComponent.Descriptor);
        var propertyActivator = new DefaultComponentPropertyActivator(resolver);
        var component = new InjectableComponent();

        propertyActivator.GetActivator(typeof(InjectableComponent))(serviceProvider, component);

        Assert.Same(serviceProvider.GetRequiredService<SomeService>(), component.Service);
        Assert.True(component.SetThroughDescriptor);
    }

    [Fact]
    public void DefaultComponentPropertyActivator_ThrowsWhenADescribedServiceIsMissing()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var resolver = CreateTypeInfoResolver(InjectableComponent.Descriptor);
        var propertyActivator = new DefaultComponentPropertyActivator(resolver);
        var activator = propertyActivator.GetActivator(typeof(InjectableComponent));

        var ex = Assert.Throws<InvalidOperationException>(() => activator(serviceProvider, new InjectableComponent()));

        Assert.Contains($"Cannot provide a value for property '{nameof(InjectableComponent.Service)}'", ex.Message);
    }

    [Fact]
    public void DefaultComponentPropertyActivator_FallsBackToReflectionForUndescribedComponents()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SomeService());
        var serviceProvider = services.BuildServiceProvider();
        var resolver = CreateTypeInfoResolver(InjectableComponent.Descriptor);
        var propertyActivator = new DefaultComponentPropertyActivator(resolver);
        var component = new UndescribedInjectableComponent();

        propertyActivator.GetActivator(typeof(UndescribedInjectableComponent))(serviceProvider, component);

        Assert.Same(serviceProvider.GetRequiredService<SomeService>(), component.Service);
    }

    [Fact]
    public void DefaultComponentPropertyActivator_RebuildsActivatorAfterTypeInfoInvalidation()
    {
        using var resolver = new ReflectionComponentTypeInfoResolver();
        var propertyActivator = new DefaultComponentPropertyActivator(resolver);

        var first = propertyActivator.GetActivator(typeof(UndescribedInjectableComponent));
        Assert.Same(first, propertyActivator.GetActivator(typeof(UndescribedInjectableComponent)));

        resolver.ClearCaches();

        Assert.NotSame(first, propertyActivator.GetActivator(typeof(UndescribedInjectableComponent)));
    }

    [Fact]
    public void RendererWithoutRegisteredResolver_UsesReflectionFallback()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new SomeService());
        using var renderer = new TestRenderer(services.BuildServiceProvider());

        var component = Assert.IsType<UndescribedInjectableComponent>(
            renderer.InstantiateComponent<UndescribedInjectableComponent>());

        Assert.NotNull(component.Service);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void StrictMode_UsesGeneratedDefaultsAndRejectsTypeBasedCustomActivators()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(ComponentMetadataFeature.SwitchName, false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var services = new ServiceCollection();
            services.AddSingleton(new SomeService());
            services.AddSingleton<IComponentMetadataResolver>(
                new StubResolver(InjectableComponent.Descriptor));
            var serviceProvider = services.BuildServiceProvider();

            using (var renderer = new TestRenderer(serviceProvider))
            {
                var component = Assert.IsType<InjectableComponent>(
                    renderer.InstantiateComponent<InjectableComponent>());
                Assert.NotNull(component.Service);
                Assert.True(component.SetThroughDescriptor);
            }

            using (var renderer = new TestRenderer(serviceProvider, new CustomComponentActivator()))
            {
                var exception = Assert.Throws<NotSupportedException>(
                    () => renderer.InstantiateComponent<InjectableComponent>());
                Assert.Contains(nameof(IComponentActivator), exception.Message);
            }

            var customPropertyServices = new ServiceCollection();
            customPropertyServices.AddSingleton(new SomeService());
            customPropertyServices.AddSingleton<IComponentMetadataResolver>(
                new StubResolver(InjectableComponent.Descriptor));
            customPropertyServices.AddSingleton<IComponentPropertyActivator>(
                new CustomComponentPropertyActivator());
            using (var renderer = new TestRenderer(customPropertyServices.BuildServiceProvider()))
            {
                var exception = Assert.Throws<NotSupportedException>(
                    () => renderer.InstantiateComponent<InjectableComponent>());
                Assert.Contains(nameof(IComponentPropertyActivator), exception.Message);
            }

            using (var renderer = new TestRenderer(new ServiceCollection().BuildServiceProvider()))
            {
                var exception = Assert.Throws<NotSupportedException>(
                    () => renderer.InstantiateComponent<DescribedComponent>());
                Assert.Contains(typeof(DescribedComponent).FullName!, exception.Message);
            }
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void StrictMode_CustomComponentUsesGeneratedMetadataToSetParameters()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(ComponentMetadataFeature.SwitchName, false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            var services = new ServiceCollection();
            services.AddSingleton<IComponentMetadataResolver>(
                new StubResolver(CustomParameterComponent.Descriptor));
            await using var renderer = new TestRenderer(services.BuildServiceProvider());
            var component = Assert.IsType<CustomParameterComponent>(
                renderer.InstantiateComponent<CustomParameterComponent>());
            var componentId = renderer.AssignRootComponentId(component);

            await renderer.RenderRootComponentAsync(
                componentId,
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(CustomParameterComponent.Value)] = "generated",
                }));

            Assert.Equal("generated", component.Value);
        }, options);
    }

    private sealed class StubResolver : IComponentMetadataResolver
    {
        private readonly Dictionary<Type, ComponentDescriptor> _descriptors;

        public StubResolver(params ComponentDescriptor[] descriptors)
        {
            _descriptors = descriptors.ToDictionary(d => d.Type);
        }

        public IReadOnlyList<ComponentDescriptor> Components => [.. _descriptors.Values];

        public bool TryGetComponentDescriptor(Type type, [NotNullWhen(true)] out ComponentDescriptor? descriptor)
            => _descriptors.TryGetValue(type, out descriptor);
    }

    private static IComponentTypeInfoResolver CreateTypeInfoResolver(
        params ComponentDescriptor[] descriptors)
        => new CompositeComponentTypeInfoResolver(
        [
            new SourceGeneratedComponentTypeInfoResolver(new StubResolver(descriptors)),
            new ReflectionComponentTypeInfoResolver(),
        ]);

    private sealed class SomeService;

    private sealed class DescribedComponent : IComponent
    {
        public bool CreatedByDescriptor { get; init; }

        public static ComponentDescriptor DescriptorWithFactory => new()
        {
            Type = typeof(DescribedComponent),
            CreateInstance = static _ => new DescribedComponent { CreatedByDescriptor = true },
        };

        public static ComponentDescriptor DescriptorWithoutFactory => new()
        {
            Type = typeof(DescribedComponent),
        };

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private sealed class InjectableComponent : IComponent
    {
        public SomeService Service { get; set; }

        public bool SetThroughDescriptor { get; private set; }

        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(InjectableComponent),
            CreateInstance = static _ => new InjectableComponent(),
            Injectables =
            [
                new ComponentInjectableDescriptor
                {
                    Name = nameof(Service),
                    ServiceType = typeof(SomeService),
                    Attribute = new InjectAttribute(),
                    SetValue = static (target, value) =>
                    {
                        var typed = (InjectableComponent)target;
                        typed.Service = (SomeService)value;
                        typed.SetThroughDescriptor = true;
                    },
                },
            ],
        };

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private sealed class UndescribedInjectableComponent : IComponent
    {
        [Inject] public SomeService Service { get; set; }

        public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

        public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
    }

    private sealed class CustomComponentActivator : IComponentActivator
    {
        public IComponent CreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType)
            => new InjectableComponent();
    }

    private sealed class CustomComponentPropertyActivator : IComponentPropertyActivator
    {
        public Action<IServiceProvider, IComponent> GetActivator(
            [DynamicallyAccessedMembers(Microsoft.AspNetCore.Internal.LinkerFlags.Component)] Type componentType)
            => static (_, _) => { };
    }

    private sealed class CustomParameterComponent : IComponent
    {
        public static ComponentDescriptor Descriptor => new()
        {
            Type = typeof(CustomParameterComponent),
            CreateInstance = static _ => new CustomParameterComponent(),
            Parameters =
            [
                new ComponentParameterDescriptor
                {
                    Name = nameof(Value),
                    ParameterType = typeof(string),
                    Attribute = new ParameterAttribute(),
                    SetValue = static (target, value) => ((CustomParameterComponent)target).Value = (string?)value,
                    GetValue = static target => ((CustomParameterComponent)target).Value,
                },
            ],
        };

        public string? Value { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);
            return Task.CompletedTask;
        }
    }
}
