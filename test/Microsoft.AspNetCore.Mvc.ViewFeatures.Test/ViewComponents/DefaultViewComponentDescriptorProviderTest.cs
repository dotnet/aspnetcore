// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentDescriptorProviderTest
    {
        [Fact]
        public void GetDescriptor_DefaultConventions()
        {
            // Arrange
            var provider = CreateProvider(typeof(ConventionsViewComponent));

            // Act
            var descriptors = provider.GetViewComponents();

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Same(typeof(ConventionsViewComponent), descriptor.Type);
            Assert.Equal("Microsoft.AspNet.Mvc.ViewComponents.Conventions", descriptor.FullName);
            Assert.Equal("Conventions", descriptor.ShortName);
            Assert.Same(typeof(ConventionsViewComponent).GetMethod("Invoke"), descriptor.MethodInfo);
        }

        [Fact]
        public void GetDescriptor_WithAttribute()
        {
            // Arrange
            var provider = CreateProvider(typeof(AttributeViewComponent));

            // Act
            var descriptors = provider.GetViewComponents();

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(AttributeViewComponent), descriptor.Type);
            Assert.Equal("AttributesAreGreat", descriptor.FullName);
            Assert.Equal("AttributesAreGreat", descriptor.ShortName);
            Assert.Same(typeof(AttributeViewComponent).GetMethod("InvokeAsync"), descriptor.MethodInfo);
        }

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

        [Fact]
        public void GetViewComponents_ThrowsIfInvokeReturnsATask()
        {
            // Arrange
            var type = typeof(TaskReturningInvokeViewComponent);
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

        private class ConventionsViewComponent
        {
            public string Invoke() => "Hello world";
        }

        [ViewComponent(Name = "AttributesAreGreat")]
        private class AttributeViewComponent
        {
            public Task<string> InvokeAsync() => Task.FromResult("Hello world");
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
                : base(GetAssemblyProvider())
            {
                AllowedTypes = allowedTypes;
            }

            public Type[] AllowedTypes { get; }

            protected override bool IsViewComponentType(TypeInfo typeInfo)
            {
                return AllowedTypes.Contains(typeInfo.AsType());
            }

            // Need to override this since the default provider does not support private classes.
            protected override IEnumerable<TypeInfo> GetCandidateTypes()
            {
                return
                    GetAssemblyProvider()
                    .CandidateAssemblies
                    .SelectMany(a => a.DefinedTypes);
            }

            private static IAssemblyProvider GetAssemblyProvider()
            {
                var assemblyProvider = new StaticAssemblyProvider();
                assemblyProvider.CandidateAssemblies.Add(
                    typeof(FilteredViewComponentDescriptorProvider).GetTypeInfo().Assembly);

                return assemblyProvider;
            }
        }
    }
}