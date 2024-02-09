// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

public class ComponentFactoryTest
{
    [Fact]
    public void InstantiateComponent_CreatesInstance()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var serviceProvider = GetServiceProvider();
        var factory = new ComponentFactory(new DefaultComponentActivator(serviceProvider), new TestRenderer());
        
        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null, null);

        // Assert
        Assert.NotNull(instance);
        Assert.IsType<EmptyComponent>(instance);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_NonComponent()
    {
        // Arrange
        var componentType = typeof(List<string>);
        var serviceProvider = GetServiceProvider();
        var factory = new ComponentFactory(new DefaultComponentActivator(serviceProvider), new TestRenderer());
        
        // Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType, null, null));
        Assert.StartsWith($"The type {componentType.FullName} does not implement {nameof(IComponent)}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_WithCustomActivator()
    {
        // Arrange
        var componentType = typeof(EmptyComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<ComponentWithInjectProperties>(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null, null);

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
        var ex = Assert.Throws<InvalidOperationException>(() => factory.InstantiateComponent(GetServiceProvider(), componentType, null, null));
        Assert.Equal($"The component activator returned a null value for a component of type {componentType.FullName}.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_AssignsPropertiesWithInjectAttributeOnBaseType()
    {
        // Arrange
        var componentType = typeof(DerivedComponent);
        var factory = new ComponentFactory(new CustomComponentActivator<DerivedComponent>(), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(GetServiceProvider(), componentType, null, null);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<DerivedComponent>(instance);
        Assert.NotNull(component.Property1);
        Assert.NotNull(component.GetProperty2());
        Assert.NotNull(component.Property3);
        Assert.NotNull(component.KeyedProperty);

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
        var serviceProvider = GetServiceProvider();
        var factory = new ComponentFactory(new DefaultComponentActivator(serviceProvider), new TestRenderer());

        // Act
        var instance = factory.InstantiateComponent(serviceProvider, componentType, null, null);

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
        var serviceProvider = GetServiceProvider();
        var factory = new ComponentFactory(new DefaultComponentActivator(serviceProvider), renderer);

        // Act
        var instance = factory.InstantiateComponent(serviceProvider, componentType, null, null);

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
        var serviceProvider = GetServiceProvider();
        var componentActivator = new DefaultComponentActivator(serviceProvider);
        var factory = new ComponentFactory(componentActivator, renderer);

        // Act
        var instance = (ComponentWithInjectProperties)factory.InstantiateComponent(serviceProvider, componentType, null, 1234);

        // Assert
        Assert.True(renderer.ResolverWasCalled);
        Assert.Same(resolvedComponent, instance);
        Assert.NotNull(instance.Property1);
        Assert.Equal(componentType, renderer.RequestedComponentType);
        Assert.Equal(1234, renderer.SuppliedParentComponentId);
        Assert.Same(componentActivator, renderer.SuppliedActivator);
        Assert.IsType<TestRenderMode>(renderer.SuppliedRenderMode);
    }

    [Fact]
    public void InstantiateComponent_WithDerivedRenderModeOnDerivedComponent_CausesAmbiguousMatchException()
    {
        // We could allow derived components to override the rendermode, but:
        // [1] It's unclear how that would be legitimate. If the base specifies a rendermode, it's saying
        //     it only works in that mode. It wouldn't be safe for a derived type to change that.
        // [2] If we did want to implement this, we'd need to implement our own inheritance chain walking
        //     to make sure we find the rendermode from the *closest* ancestor type. GetCustomAttributes
        //     on its own isn't documented to return the results in any specific order.
        // Since issue [1] makes it unclear we'd want to support this, for now we don't.

        // Arrange
        var resolvedComponent = new ComponentWithInjectProperties();
        var componentType = typeof(DerivedComponentWithRenderMode);
        var renderer = new RendererWithResolveComponentForRenderMode(resolvedComponent);
        var serviceProvider = GetServiceProvider();
        var componentActivator = new DefaultComponentActivator(serviceProvider);
        var factory = new ComponentFactory(componentActivator, renderer);

        // Act/Assert
        Assert.Throws<AmbiguousMatchException>(
            () => factory.InstantiateComponent(serviceProvider, componentType, null, 1234));
    }

    [Fact]
    public void InstantiateComponent_WithRenderModeOnCallSite_UsesRenderModeResolver()
    {
        // Arrange
        // Notice that the requested component type is not the same as the resolved component type. This
        // is intentional and shows that component factories are allowed to substitute other component types.
        var resolvedComponent = new ComponentWithInjectProperties();
        var componentType = typeof(ComponentWithNonInjectableProperties);
        var callSiteRenderMode = new TestRenderMode();
        var renderer = new RendererWithResolveComponentForRenderMode(resolvedComponent);
        var serviceProvider = GetServiceProvider();
        var componentActivator = new DefaultComponentActivator(serviceProvider);
        var factory = new ComponentFactory(componentActivator, renderer);

        // Act
        var instance = (ComponentWithInjectProperties)factory.InstantiateComponent(serviceProvider, componentType, callSiteRenderMode, 1234);

        // Assert
        Assert.Same(resolvedComponent, instance);
        Assert.NotNull(instance.Property1);
        Assert.Equal(componentType, renderer.RequestedComponentType);
        Assert.Same(componentActivator, renderer.SuppliedActivator);
        Assert.Same(callSiteRenderMode, renderer.SuppliedRenderMode);
        Assert.Equal(1234, renderer.SuppliedParentComponentId);
    }

    [Fact]
    public void InstantiateComponent_WithRenderModeOnComponentAndCallSite_Throws()
    {
        // Arrange
        var resolvedComponent = new ComponentWithInjectProperties();
        var componentType = typeof(ComponentWithRenderMode);
        var renderer = new RendererWithResolveComponentForRenderMode(resolvedComponent);
        var serviceProvider = GetServiceProvider();
        var componentActivator = new DefaultComponentActivator(serviceProvider);
        var factory = new ComponentFactory(componentActivator, renderer);

        // Even though the two rendermodes are literally the same object, we don't allow specifying any nonnull
        // rendermode at the callsite if there's a nonnull fixed rendermode
        var callsiteRenderMode = componentType.GetCustomAttribute<RenderModeAttribute>().Mode;

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            factory.InstantiateComponent(GetServiceProvider(), componentType, callsiteRenderMode, 1234));
        Assert.Equal($"The component type '{componentType}' has a fixed rendermode of '{typeof(TestRenderMode)}', so it is not valid to specify any rendermode when using this component.", ex.Message);
    }

    [Fact]
    public void InstantiateComponent_CreatesInstance_WithTypeActivation()
    {
        // Arrange
        var serviceProvider = GetServiceProvider();
        var componentType = typeof(ComponentWithConstructorInjection);
        var resolvedComponent = new ComponentWithInjectProperties();
        var renderer = new RendererWithResolveComponentForRenderMode(resolvedComponent);
        var defaultComponentActivator = new DefaultComponentActivator(serviceProvider);
        var factory = new ComponentFactory(defaultComponentActivator, renderer);

        // Act
        var instance = factory.InstantiateComponent(serviceProvider, componentType, null, null);

        // Assert
        Assert.NotNull(instance);
        var component = Assert.IsType<ComponentWithConstructorInjection>(instance);
        Assert.NotNull(component.Property1);
        Assert.NotNull(component.Property2);
        Assert.NotNull(component.Property3); // Property injection should still work.
    }

    private const string KeyedServiceKey = "my-keyed-service";

    private static IServiceProvider GetServiceProvider()
    {
        return new ServiceCollection()
            .AddTransient<TestService1>()
            .AddTransient<TestService2>()
            .AddKeyedTransient<TestService3>(KeyedServiceKey)
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

        [Inject(Key = KeyedServiceKey)]
        public TestService3 KeyedProperty { get; set; }

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

    public class ComponentWithConstructorInjection : IComponent
    {
        public ComponentWithConstructorInjection(TestService1 property1, TestService2 property2)
        {
            Property1 = property1;
            Property2 = property2;
        }

        public TestService1 Property1 { get; }
        public TestService2 Property2 { get; }

        [Inject]
        public TestService2 Property3 { get; set; }

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
    public class TestService3 { }

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
    private class DerivedComponentRenderMode : IComponentRenderMode { }

    [DerivedComponentRenderMode]
    private class DerivedComponentWithRenderMode : ComponentWithRenderMode
    {
        class DerivedComponentRenderModeAttribute : RenderModeAttribute
        {
            public override IComponentRenderMode Mode => new DerivedComponentRenderMode();
        }
    }

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
        public IComponentRenderMode SuppliedRenderMode { get; private set; }

        public override Dispatcher Dispatcher => throw new NotImplementedException();

        protected override void HandleException(Exception exception)
        {
            throw new NotImplementedException();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            throw new NotImplementedException();
        }

        protected internal override IComponent ResolveComponentForRenderMode(Type componentType, int? parentComponentId, IComponentActivator componentActivator, IComponentRenderMode renderMode)
        {
            ResolverWasCalled = true;
            RequestedComponentType = componentType;
            SuppliedParentComponentId = parentComponentId;
            SuppliedActivator = componentActivator;
            SuppliedRenderMode = renderMode;
            return _componentToReturn;
        }
    }
}
