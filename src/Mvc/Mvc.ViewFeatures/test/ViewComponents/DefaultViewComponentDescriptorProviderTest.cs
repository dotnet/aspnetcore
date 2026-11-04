// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

public class DefaultViewComponentDescriptorProviderTest
{
    [Theory]
    [InlineData(typeof(NoMethodsViewComponent))]
    [InlineData(typeof(NonPublicInvokeAsyncViewComponent))]
    [InlineData(typeof(NonPublicInvokeViewComponent))]
    public void GetViewComponents_ThrowsIfTypeHasNoInvocationMethods(Type type)
    {
        // Arrange
        var expected = $"Could not find an 'Invoke' or 'InvokeAsync' method for the view component '{type}'.";
        var provider = CreateProvider(type);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetViewComponents().ToArray());
        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData(typeof(MultipleInvokeViewComponent))]
    [InlineData(typeof(MultipleInvokeAsyncViewComponent))]
    [InlineData(typeof(InvokeAndInvokeAsyncViewComponent))]
    public void GetViewComponents_ThrowsIfTypeHasAmbiguousInvocationMethods(Type type)
    {
        // Arrange
        var expected = $"View component '{type}' must have exactly one public method named " +
            "'InvokeAsync' or 'Invoke'.";
        var provider = CreateProvider(type);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetViewComponents().ToArray());
        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData(typeof(NonGenericTaskReturningInvokeAsyncViewComponent))]
    [InlineData(typeof(VoidReturningInvokeAsyncViewComponent))]
    [InlineData(typeof(NonTaskReturningInvokeAsyncViewComponent))]
    public void GetViewComponents_ThrowsIfInvokeAsyncDoesNotHaveCorrectReturnType(Type type)
    {
        // Arrange
        var expected = $"Method 'InvokeAsync' of view component '{type}' should be declared to return Task<T>.";
        var provider = CreateProvider(type);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetViewComponents().ToArray());
        Assert.Equal(expected, ex.Message);
    }

    [Theory]
    [InlineData(typeof(TaskReturningInvokeViewComponent))]
    [InlineData(typeof(GenericTaskReturningInvokeViewComponent))]
    public void GetViewComponents_ThrowsIfInvokeReturnsATask(Type type)
    {
        // Arrange
        var expected = $"Method 'Invoke' of view component '{type}' cannot return a Task.";
        var provider = CreateProvider(type);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetViewComponents().ToArray());
        Assert.Equal(expected, ex.Message);
    }

    [Fact]
    public void GetViewComponents_ThrowsIfInvokeIsVoidReturning()
    {
        // Arrange
        var type = typeof(VoidReturningInvokeViewComponent);
        var expected = $"Method 'Invoke' of view component '{type}' should be declared to return a value.";
        var provider = CreateProvider(type);

        // Act and Assert
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetViewComponents().ToArray());
        Assert.Equal(expected, ex.Message);
    }

    private class MultipleInvokeViewComponent
    {
        public IViewComponentResult Invoke() => null;

        public IViewComponentResult Invoke(int a) => null;
    }

    private class NoMethodsViewComponent
    {
    }

    private class NonPublicInvokeViewComponent
    {
        private IViewComponentResult Invoke() => null;
    }

    private class NonPublicInvokeAsyncViewComponent
    {
        protected Task<IViewComponentResult> InvokeAsync() => null;
    }

    private class MultipleInvokeAsyncViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(string a) => null;

        public Task<IViewComponentResult> InvokeAsync(int a) => null;

        public Task<IViewComponentResult> InvokeAsync(int a, int b) => null;
    }

    private class InvokeAndInvokeAsyncViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync(string a) => null;

        public string InvokeAsync(int a) => null;
    }

    private class NonGenericTaskReturningInvokeAsyncViewComponent
    {
        public Task InvokeAsync() => Task.FromResult(0);
    }

    private class VoidReturningInvokeAsyncViewComponent
    {
        public void InvokeAsync()
        {
        }
    }

    public class NonTaskReturningInvokeAsyncViewComponent
    {
        public long InvokeAsync() => 0L;
    }

    public class TaskReturningInvokeViewComponent
    {
        public Task Invoke() => Task.FromResult(0);
    }

    public class GenericTaskReturningInvokeViewComponent
    {
        public Task<int> Invoke() => Task.FromResult(0);
    }

    public class VoidReturningInvokeViewComponent
    {
        public void Invoke(int x)
        {
        }
    }

    private DefaultViewComponentDescriptorProvider CreateProvider(Type componentType)
    {
        return new FilteredViewComponentDescriptorProvider(componentType);
    }

    // This will only consider types nested inside this class as ViewComponent classes
    private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
    {
        public FilteredViewComponentDescriptorProvider(params Type[] allowedTypes)
            : base(GetApplicationPartManager(allowedTypes.Select(t => t.GetTypeInfo())))
        {
        }

        private static ApplicationPartManager GetApplicationPartManager(IEnumerable<TypeInfo> types)
        {
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(types));
            manager.FeatureProviders.Add(new TestFeatureProvider());
            return manager;
        }

        private class TestFeatureProvider : IApplicationFeatureProvider<ViewComponentFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
                {
                    feature.ViewComponents.Add(type);
                }
            }
        }
    }
}
