// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ViewComponentTagHelperDescriptorFactoryTest
    {
        private static readonly Assembly _assembly = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetTypeInfo().Assembly;

        [Fact]
        public void CreateDescriptor_UnderstandsStringParameters()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(StringParameterViewComponent).FullName);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);
            var expectedDescriptor = TagHelperDescriptorBuilder.Create(
                "__Generated__StringParameterViewComponentTagHelper",
                typeof(StringParameterViewComponent).GetTypeInfo().Assembly.GetName().Name)
                .DisplayName("StringParameterViewComponentTagHelper")
                .TagMatchingRule(rule =>
                    rule
                    .RequireTagName("vc:string-parameter")
                    .RequireAttribute(attribute => attribute.Name("foo"))
                    .RequireAttribute(attribute => attribute.Name("bar")))
                .BindAttribute(attribute =>
                    attribute
                    .Name("foo")
                    .PropertyName("foo")
                    .TypeName(typeof(string).FullName)
                    .DisplayName("string StringParameterViewComponentTagHelper.foo"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("bar")
                    .PropertyName("bar")
                    .TypeName(typeof(string).FullName)
                    .DisplayName("string StringParameterViewComponentTagHelper.bar"))
                .AddMetadata(ViewComponentTypes.ViewComponentNameKey, "StringParameter")
                .Build();

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void CreateDescriptor_UnderstandsVariousParameterTypes()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(VariousParameterViewComponent).FullName);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);
            var expectedDescriptor = TagHelperDescriptorBuilder.Create(
                "__Generated__VariousParameterViewComponentTagHelper",
                typeof(VariousParameterViewComponent).GetTypeInfo().Assembly.GetName().Name)
                .DisplayName("VariousParameterViewComponentTagHelper")
                .TagMatchingRule(rule =>
                    rule
                    .RequireTagName("vc:various-parameter")
                    .RequireAttribute(attribute => attribute.Name("test-enum"))
                    .RequireAttribute(attribute => attribute.Name("test-string"))
                    .RequireAttribute(attribute => attribute.Name("baz")))
                .BindAttribute(attribute =>
                    attribute
                    .Name("test-enum")
                    .PropertyName("testEnum")
                    .TypeName(typeof(VariousParameterViewComponent).FullName + "." + nameof(VariousParameterViewComponent.TestEnum))
                    .AsEnum()
                    .DisplayName(typeof(VariousParameterViewComponent).FullName + "." + nameof(VariousParameterViewComponent.TestEnum) + " VariousParameterViewComponentTagHelper.testEnum"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("test-string")
                    .PropertyName("testString")
                    .TypeName(typeof(string).FullName)
                    .DisplayName("string VariousParameterViewComponentTagHelper.testString"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("baz")
                    .PropertyName("baz")
                    .TypeName(typeof(int).FullName)
                    .DisplayName("int VariousParameterViewComponentTagHelper.baz"))
                .AddMetadata(ViewComponentTypes.ViewComponentNameKey, "VariousParameter")
                .Build();

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void CreateDescriptor_UnderstandsGenericParameters()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(GenericParameterViewComponent).FullName);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);
            var expectedDescriptor = TagHelperDescriptorBuilder.Create(
                "__Generated__GenericParameterViewComponentTagHelper",
                typeof(GenericParameterViewComponent).GetTypeInfo().Assembly.GetName().Name)
                .DisplayName("GenericParameterViewComponentTagHelper")
                .TagMatchingRule(rule =>
                    rule
                    .RequireTagName("vc:generic-parameter")
                    .RequireAttribute(attribute => attribute.Name("foo")))
                .BindAttribute(attribute =>
                    attribute
                    .Name("foo")
                    .PropertyName("Foo")
                    .TypeName("System.Collections.Generic.List<System.String>")
                    .DisplayName("System.Collections.Generic.List<System.String> GenericParameterViewComponentTagHelper.Foo"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("bar")
                    .PropertyName("Bar")
                    .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                    .AsDictionary("bar-", typeof(int).FullName)
                    .DisplayName("System.Collections.Generic.Dictionary<System.String, System.Int32> GenericParameterViewComponentTagHelper.Bar"))
                .AddMetadata(ViewComponentTypes.ViewComponentNameKey, "GenericParameter")
                .Build();

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void CreateDescriptor_AddsDiagnostic_ForViewComponentWithNoInvokeMethod()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(ViewComponentWithoutInvokeMethod).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_CannotFindMethod.Id, diagnostic.Id);
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvokeAsync_UnderstandsGenericTask()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(AsyncViewComponentWithGenericTask).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Empty(descriptor.GetAllDiagnostics());
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvokeAsync_UnderstandsNonGenericTask()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(AsyncViewComponentWithNonGenericTask).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Empty(descriptor.GetAllDiagnostics());
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvokeAsync_DoesNotUnderstandVoid()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(AsyncViewComponentWithString).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_AsyncMethod_ShouldReturnTask.Id, diagnostic.Id);
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvokeAsync_DoesNotUnderstandString()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(AsyncViewComponentWithString).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_AsyncMethod_ShouldReturnTask.Id, diagnostic.Id);
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandVoid()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(SyncViewComponentWithVoid).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_SyncMethod_ShouldReturnValue.Id, diagnostic.Id);
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandNonGenericTask()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(SyncViewComponentWithNonGenericTask).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_SyncMethod_CannotReturnTask.Id, diagnostic.Id);
        }

        [Fact]
        public void CreateDescriptor_ForViewComponentWithInvoke_DoesNotUnderstandGenericTask()
        {
            // Arrange
            var testCompilation = TestCompilation.Create(_assembly);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);

            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(SyncViewComponentWithGenericTask).FullName);

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            var diagnostic = Assert.Single(descriptor.GetAllDiagnostics());
            Assert.Equal(ViewComponentDiagnosticFactory.ViewComponent_SyncMethod_CannotReturnTask.Id, diagnostic.Id);
        }
    }

    public class StringParameterViewComponent
    {
        public string Invoke(string foo, string bar) => null;
    }

    public class VariousParameterViewComponent
    {
        public string Invoke(TestEnum testEnum, string testString, int baz = 5) => null;

        public enum TestEnum
        {
            A = 1,
            B = 2,
            C = 3
        }
    }

    public class GenericParameterViewComponent
    {
        public string Invoke(List<string> Foo, Dictionary<string, int> Bar) => null;
    }

    public class ViewComponentWithoutInvokeMethod
    {
    }

    public class AsyncViewComponentWithGenericTask
    {
        public Task<string> InvokeAsync() => null;
    }

    public class AsyncViewComponentWithNonGenericTask
    {
        public Task InvokeAsync() => null;
    }

    public class AsyncViewComponentWithVoid
    {
        public void InvokeAsync() { }
    }

    public class AsyncViewComponentWithString
    {
        public string InvokeAsync() => null;
    }

    public class SyncViewComponentWithVoid
    {
        public void Invoke() { }
    }

    public class SyncViewComponentWithNonGenericTask
    {
        public Task Invoke() => null;
    }

    public class SyncViewComponentWithGenericTask
    {
        public Task<string> Invoke() => null;
    }
}