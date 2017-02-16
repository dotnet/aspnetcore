// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.CodeAnalysis.Razor.Workspaces.Test;
using Microsoft.CodeAnalysis.Razor.Workspaces.Test.Comparers;
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
            var expectedDescriptor = new TagHelperDescriptor
            {
                TagName = "vc:string-parameter",
                TypeName = "__Generated__StringParameterViewComponentTagHelper",
                AssemblyName = typeof(StringParameterViewComponent).GetTypeInfo().Assembly.GetName().Name,
                Attributes = new List<TagHelperAttributeDescriptor>
                {
                    new TagHelperAttributeDescriptor
                    {
                        Name = "foo",
                        PropertyName = "foo",
                        TypeName = typeof(string).FullName
                    },
                    new TagHelperAttributeDescriptor
                    {
                        Name = "bar",
                        PropertyName = "bar",
                        TypeName = typeof(string).FullName
                    }
                },
                RequiredAttributes = new List<TagHelperRequiredAttributeDescriptor>
                {
                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "foo"
                    },
                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "bar"
                    }
                }
            };
            expectedDescriptor.PropertyBag.Add(ViewComponentTypes.ViewComponentNameKey, "StringParameter");

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_UnderstandsVariousParameterTypes()
        {
            // Arrange
            var testCompilation = TestCompilation.Create();
            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(VariousParameterViewComponent).FullName);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);
            var expectedDescriptor = new TagHelperDescriptor
            {
                TagName = "vc:various-parameter",
                TypeName = "__Generated__VariousParameterViewComponentTagHelper",
                AssemblyName = typeof(VariousParameterViewComponent).GetTypeInfo().Assembly.GetName().Name,
                Attributes = new List<TagHelperAttributeDescriptor>
                {
                    new TagHelperAttributeDescriptor
                    {
                        Name = "test-enum",
                        PropertyName = "testEnum",
                        TypeName = typeof(VariousParameterViewComponent).FullName + "." + nameof(VariousParameterViewComponent.TestEnum),
                        IsEnum = true
                    },

                    new TagHelperAttributeDescriptor
                    {
                        Name = "test-string",
                        PropertyName = "testString",
                        TypeName = typeof(string).FullName
                    },

                    new TagHelperAttributeDescriptor
                    {
                        Name = "baz",
                        PropertyName = "baz",
                        TypeName = typeof(int).FullName
                    }
                },
                RequiredAttributes = new List<TagHelperRequiredAttributeDescriptor>
                {
                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "test-enum"
                    },

                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "test-string"
                    },

                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "baz"
                    }
                }
            };
            expectedDescriptor.PropertyBag.Add(ViewComponentTypes.ViewComponentNameKey, "VariousParameter");

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void CreateDescriptor_UnderstandsGenericParameters()
        {
            // Arrange
            var testCompilation = TestCompilation.Create();
            var viewComponent = testCompilation.GetTypeByMetadataName(typeof(GenericParameterViewComponent).FullName);
            var factory = new ViewComponentTagHelperDescriptorFactory(testCompilation);
            var expectedDescriptor = new TagHelperDescriptor
            {
                TagName = "vc:generic-parameter",
                TypeName = "__Generated__GenericParameterViewComponentTagHelper",
                AssemblyName = typeof(GenericParameterViewComponent).GetTypeInfo().Assembly.GetName().Name,
                Attributes = new List<TagHelperAttributeDescriptor>
                {
                    new TagHelperAttributeDescriptor
                    {
                        Name = "foo",
                        PropertyName = "Foo",
                        TypeName = "System.Collections.Generic.List<System.String>"
                    },

                    new TagHelperAttributeDescriptor
                    {
                        Name = "bar",
                        PropertyName = "Bar",
                        TypeName = "System.Collections.Generic.Dictionary<System.String, System.Int32>"
                    },

                    new TagHelperAttributeDescriptor
                    {
                        Name = "bar-",
                        PropertyName = "Bar",
                        TypeName = typeof(int).FullName,
                        IsIndexer = true
                    }
                },
                RequiredAttributes = new List<TagHelperRequiredAttributeDescriptor>
                {
                    new TagHelperRequiredAttributeDescriptor
                    {
                        Name = "foo"
                    }
                }
            };
            expectedDescriptor.PropertyBag.Add(ViewComponentTypes.ViewComponentNameKey, "GenericParameter");

            // Act
            var descriptor = factory.CreateDescriptor(viewComponent);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, CaseSensitiveTagHelperDescriptorComparer.Default);
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