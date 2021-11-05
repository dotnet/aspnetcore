// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Integration tests for component directives
public class ComponentDirectiveIntegrationTest : RazorIntegrationTestBase
{
    public ComponentDirectiveIntegrationTest()
    {
        // Include this assembly to use types defined in tests.
        BaseCompilation = DefaultBaseCompilation.AddReferences(MetadataReference.CreateFromFile(GetType().Assembly.Location));
    }

    internal override CSharpCompilation BaseCompilation { get; }

    internal override string FileKind => FileKinds.Component;

    [Fact]
    public void ComponentsDoNotHaveLayoutAttributeByDefault()
    {
        // Arrange/Act
        var component = CompileToComponent("Hello");

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
            "Hello");

        // Assert
        var layoutAttribute = component.GetType().GetCustomAttribute<LayoutAttribute>();
        Assert.NotNull(layoutAttribute);
    }

    [Fact]
    public void SupportsImplementsDeclarations()
    {
        // Arrange/Act
        var testInterfaceTypeName = FullTypeName<ITestInterface>();
        var component = CompileToComponent(
            $"@implements {testInterfaceTypeName}\n" +
            "Hello");

        // Assert
        Assert.IsAssignableFrom<ITestInterface>(component);
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
            "Hello");

        // Assert
        Assert.IsAssignableFrom<ITestInterface>(component);
        Assert.IsAssignableFrom<ITestInterface2>(component);
    }

    [Fact]
    public void SupportsInheritsDirective()
    {
        // Arrange/Act
        var testBaseClassTypeName = FullTypeName<TestBaseClass>();
        var component = CompileToComponent(
            $"@inherits {testBaseClassTypeName}" + Environment.NewLine +
            "Hello");

        // Assert
        Assert.IsAssignableFrom<TestBaseClass>(component);
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
    }

    public class TestLayout : IComponent
    {
        [Parameter]
        public RenderFragment Body { get; set; }

        public void Attach(RenderHandle renderHandle)
        {
            throw new NotImplementedException();
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            throw new NotImplementedException();
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
