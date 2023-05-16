// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

public class ComponentFactoryTest
{
    [Fact]
    public void InstantiateComponent_CreatesInstance()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new DefaultComponentActivator(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null);

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<EmptyComponent>(instance);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_NonComponent()
    {
        // Arrange
        var componentType = typeof(List<string>);
        var factory = new ComponentFactory(new DefaultComponentActivator(), new TestRenderer());

        // Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType, null));
        Assert.StartsWith($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_WithCustomActivator()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<ComponentWithInjectProperties>(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<ComponentWithInjectProperties>(instance); // Custom activator returns a different type

        // Public, and non-public properties, and properties with non-public setters should get assigned
        Assert.NotNull(component.Property1);
        Assert.NotNull(component.GetProperty2());
        Assert.NotNull(component.Property3);
        Assert.NotNull(component.Property4);
    }

    [Fact]
    public void InstantiateComponent_ThrowsForNullInstance()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new NullResultComponentActivator(), new TestRenderer());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType, null));
        Assert.Equal($"The component activator returned a null value for a component of type {componentType.FullName}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_AssignsPropertiesWithInjectAttributeOnBaseType()
    {
        // Arrange
        var componentType = typeof(DerivedComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<DerivedComponent>(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<DerivedComponent>(instance);
        Assert.NotNull(component.Property1);
        Assert.NotNull(component.GetProperty2());
        Assert.NotNull(component.Property3);

        // Property on derived type without [Inject] should not be assigned
        Assert.Null(component.Property4);
        // Property on the base type with the [Inject] attribute should
        Assert.NotNull(((ComponentWithInjectProperties)component).Property4);
    }

    [Fact]
    public void InstantiateComponent_IgnoresPropertiesWithoutInjectAttribute()
    {
        // Arrange
        var componentType = typeof(ComponentWithNonInjectableProperties);
        var factory = new ComponentFactory(new DefaultComponentActivator(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<ComponentWithNonInjectableProperties>(instance);
        // Public, and non-public properties, and properties with non-public setters should get assigned
        Assert.NotNull(component.Property1);
        Assert.Null(component.Property2);
    }

    [Fact]
    public void InstantiateComponent_WithNoRenderMode_DoesNotUseRenderModeResolver()
    {
        // Arrange
        var componentType = typeof(ComponentWithInjectProperties);
        var renderer = new RendererWithResolveComponentForRenderMode(
            /* won't be used */ new ComponentWithRenderMode());
        var factory = new ComponentFactory(new DefaultComponentActivator(), renderer);

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null);

        // Assert
        Assert.IsType<ComponentWithInjectProperties>(instance);
        Assert.False(renderer.ResolverWasCalled);
    }

    [Fact]
    public void InstantiateComponent_WithRenderModeOnComponent_UsesRenderModeResolver()
    {
        // Arrange
        var resolvedComponent = new ComponentWithInjectProperties();
        var componentType = typeof(ComponentWithRenderMode);
        var renderer = new RendererWithResolveComponentForRenderMode(resolvedComponent);
        var componentActivator = new DefaultComponentActivator();
        var factory = new ComponentFactory(componentActivator, renderer);

        // Act
        var instance = (ComponentWithInjectProperties)factory.InstantiateComponent(GetServiceProvider(), componentType, 1234);

        // Assert
        Assert.True(renderer.ResolverWasCalled);
        Assert.Same(resolvedComponent, instance);
        Assert.NotNull(instance.Property1);
        Assert.Equal(componentType, renderer.RequestedComponentType);
        Assert.Equal(1234, renderer.SuppliedParentComponentId);
        Assert.Same(componentActivator, renderer.SuppliedActivator);
        Assert.IsType<TestRenderMode>(renderer.SuppliedComponentTypeRenderMode);
    }

    private static IServiceProvider GetServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<TestService1>()
            .AddTransient<TestService2>()
            .BuildServiceProvider();
    }

    private class EmptyComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    private class ComponentWithInjectProperties : IComponent
    {
        [Inject]
        public TestService1 Property1 { get; set; }

        [Inject]
        private TestService2 Property2 { get; set; }

        [Inject]
        public TestService1 Property3 { get; private set; }

        [Inject]
        public TestService1 Property4 { get; set; }

        public TestService2 GetProperty2() => Property2;

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    private class ComponentWithNonInjectableProperties : IComponent
    {
        [Inject]
        public TestService1 Property1 { get; set; }

        public TestService1 Property2 { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }
    }

    private class DerivedComponent : ComponentWithInjectProperties
    {
        public new TestService2 Property4 { get; set; }

        [Inject]
        public TestService2 Property5 { get; set; }
    }

    public class TestService1 { }
    public class TestService2 { }

    private class CustomComponentActivator<TResult> : IComponentActivator where TResult : IComponent, new()
    {
        public IComponent CreateInstance(Type componentType)
        {
            return new TResult();
        }
    }

    private class NullResultComponentActivator : IComponentActivator
    {
        public IComponent CreateInstance(Type componentType)
        {
            return null;
        }
    }

    private class TestRenderMode : IComponentRenderMode { }

    [OwnRenderMode]
    private class ComponentWithRenderMode : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
        }

        class OwnRenderMode : RenderModeAttribute
        {
            public override IComponentRenderMode Mode => new TestRenderMode();
        }
    }

    private class RendererWithResolveComponentForRenderMode : TestRenderer
    {
        private readonly IComponent _componentToReturn;

        public RendererWithResolveComponentForRenderMode(IComponent componentToReturn) : base()
        {
            _componentToReturn = componentToReturn;
        }

        public bool ResolverWasCalled { get; private set; }
        public Type RequestedComponentType { get; private set; }
        public int? SuppliedParentComponentId { get; private set; }
        public IComponentActivator SuppliedActivator { get; private set; }
        public IComponentRenderMode SuppliedComponentTypeRenderMode { get; private set; }

        public override Dispatcher Dispatcher => throw new NotImplementedException();

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }

        protected internal override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode componentTypeRenderMode)
        {
            ResolverWasCalled = true;
            RequestedComponentType = componentType;
            SuppliedParentComponentId = parentComponentId;
            SuppliedActivator = componentActivator;
            SuppliedComponentTypeRenderMode = componentTypeRenderMode;
            return _componentToReturn;
        }
    }
}
