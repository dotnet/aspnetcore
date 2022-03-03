// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class ComponentFactoryTest
{
    [Fact]
    public void InstantiateComponent_CreatesInstance()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new DefaultComponentActivator());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType);

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<EmptyComponent>(instance);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_NonComponent()
    {
        // Arrange
        var componentType = typeof(List<string>);
        var factory = new ComponentFactory(new DefaultComponentActivator());

        // Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType));
        Assert.StartsWith($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_WithCustomActivator()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<ComponentWithInjectProperties>());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType);

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
        var factory = new ComponentFactory(new NullResultComponentActivator());

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType));
        Assert.Equal($"The component activator returned a null value for a component of type {componentType.FullName}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_AssignsPropertiesWithInjectAttributeOnBaseType()
    {
        // Arrange
        var componentType = typeof(DerivedComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<DerivedComponent>());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType);

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
        var factory = new ComponentFactory(new DefaultComponentActivator());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<ComponentWithNonInjectableProperties>(instance);
        // Public, and non-public properties, and properties with non-public setters should get assigned
        Assert.NotNull(component.Property1);
        Assert.Null(component.Property2);
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
}
