// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    // Integration tests for Blazor's directives
    public class DirectiveRazorIntegrationTest : RazorIntegrationTestBase
    {
        public DirectiveRazorIntegrationTest(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void ComponentsDoNotHaveLayoutAttributeByDefault()
        {
            // Arrange/Act
            var component = CompileToComponent($"Hello");

            // Assert
            Assert.Null(component.GetType().GetCustomAttribute<LayoutAttribute>());
        }

        [Fact]
        public void SupportsLayoutDeclarations()
        {
            // Arrange/Act
            var testComponentTypeName = FullTypeName<TestLayout>();
            var component = CompileToComponent(
                $"@layout {testComponentTypeName}\n" +
                $"Hello");
            var frames = GetRenderTree(component);

            // Assert
            var layoutAttribute = component.GetType().GetCustomAttribute<LayoutAttribute>();
            Assert.NotNull(layoutAttribute);
            Assert.Equal(typeof(TestLayout), layoutAttribute.LayoutType);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello"));
        }

        [Fact]
        public void SupportsImplementsDeclarations()
        {
            // Arrange/Act
            var testInterfaceTypeName = FullTypeName<ITestInterface>();
            var component = CompileToComponent(
                $"@implements {testInterfaceTypeName}\n" +
                $"Hello");
            var frames = GetRenderTree(component);

            // Assert
            Assert.IsAssignableFrom<ITestInterface>(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello"));
        }

        [Fact]
        public void SupportsMultipleImplementsDeclarations()
        {
            // Arrange/Act
            var testInterfaceTypeName = FullTypeName<ITestInterface>();
            var testInterfaceTypeName2 = FullTypeName<ITestInterface2>();
            var component = CompileToComponent(
                $"@implements {testInterfaceTypeName}\n" +
                $"@implements {testInterfaceTypeName2}\n" +
                $"Hello");
            var frames = GetRenderTree(component);

            // Assert
            Assert.IsAssignableFrom<ITestInterface>(component);
            Assert.IsAssignableFrom<ITestInterface2>(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello"));
        }

        [Fact]
        public void SupportsInheritsDirective()
        {
            // Arrange/Act
            var testBaseClassTypeName = FullTypeName<TestBaseClass>();
            var component = CompileToComponent(
                $"@inherits {testBaseClassTypeName}" + Environment.NewLine +
                $"Hello");
            var frames = GetRenderTree(component);

            // Assert
            Assert.IsAssignableFrom<TestBaseClass>(component);
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello"));
        }

        [Fact]
        public void SupportsInjectDirective()
        {
            // Arrange/Act 1: Compilation
            var componentType = CompileToComponent(
                $"@inject {FullTypeName<IMyService1>()} MyService1\n" +
                $"@inject {FullTypeName<IMyService2>()} MyService2\n" +
                $"Hello from @MyService1 and @MyService2").GetType();

            // Assert 1: Compiled type has correct properties
            var propertyFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            var injectableProperties = componentType.GetProperties(propertyFlags)
                .Where(p => p.GetCustomAttribute<InjectAttribute>() != null);
            Assert.Collection(injectableProperties.OrderBy(p => p.Name),
                property =>
                {
                    Assert.Equal("MyService1", property.Name);
                    Assert.Equal(typeof(IMyService1), property.PropertyType);
                    Assert.False(property.GetMethod.IsPublic);
                    Assert.False(property.SetMethod.IsPublic);
                },
                property =>
                {
                    Assert.Equal("MyService2", property.Name);
                    Assert.Equal(typeof(IMyService2), property.PropertyType);
                    Assert.False(property.GetMethod.IsPublic);
                    Assert.False(property.SetMethod.IsPublic);
                });

            // Arrange/Act 2: DI-supplied component has correct behavior
            var serviceProvider = new TestServiceProvider();
            serviceProvider.AddService<IMyService1>(new MyService1Impl());
            serviceProvider.AddService<IMyService2>(new MyService2Impl());
            var componentFactory = new ComponentFactory();
            var component = componentFactory.InstantiateComponent(serviceProvider, componentType);
            var frames = GetRenderTree(component);

            // Assert 2: Rendered component behaves correctly
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "Hello from "),
                frame => AssertFrame.Text(frame, typeof(MyService1Impl).FullName),
                frame => AssertFrame.Text(frame, " and "),
                frame => AssertFrame.Text(frame, typeof(MyService2Impl).FullName));
        }

        public class TestLayout : IComponent
        {
            [Parameter]
            public RenderFragment Body { get; set; }

            public void Attach(RenderHandle renderHandle)
            {
            }

            public Task SetParametersAsync(ParameterView parameters)
            {
                return Task.CompletedTask;
            }
        }

        public interface ITestInterface { }

        public interface ITestInterface2 { }

        public class TestBaseClass : ComponentBase { }

        public interface IMyService1 { }
        public interface IMyService2 { }
        public class MyService1Impl : IMyService1 { }
        public class MyService2Impl : IMyService2 { }
    }
}
