// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    public class ViewComponentTagHelperDescriptorFactoryTest
    {
        [Fact]
        public void CreateDescriptor_UnderstandsStringParameters()
        {
            // Arrange
            var testCompilation = TestCompilation.Create();
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
                    .TypeName(typeof(string).FullName))
                .BindAttribute(attribute =>
                    attribute
                    .Name("bar")
                    .PropertyName("bar")
                    .TypeName(typeof(string).FullName))
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
            var testCompilation = TestCompilation.Create();
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
                    .AsEnum())
                .BindAttribute(attribute =>
                    attribute
                    .Name("test-string")
                    .PropertyName("testString")
                    .TypeName(typeof(string).FullName))
                .BindAttribute(attribute =>
                    attribute
                    .Name("baz")
                    .PropertyName("baz")
                    .TypeName(typeof(int).FullName))
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
            var testCompilation = TestCompilation.Create();
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
                    .TypeName("System.Collections.Generic.List<System.String>"))
                .BindAttribute(attribute =>
                    attribute
                    .Name("bar")
                    .PropertyName("Bar")
                    .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                    .AsDictionary("bar-", typeof(int).FullName))
                .AddMetadata(ViewComponentTypes.ViewComponentNameKey, "GenericParameter")
                .Build();

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.CaseSensitive);
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
}