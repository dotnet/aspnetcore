// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Test.Internal
{
    public class ViewComponentTagHelperDescriptorFactoryTest
    {
        public static TheoryData AssemblyData
        {
            get
            {
                var provider = new TestViewComponentDescriptorProvider();

                var assemblyOne = "Microsoft.AspNetCore.Mvc.Razor";
                var assemblyTwo = "Microsoft.AspNetCore.Mvc.Razor.Test";
                var assemblyNone = string.Empty;

                return new TheoryData<string, IEnumerable<TagHelperDescriptor>>
                {
                    { assemblyOne, new [] { provider.GetTagHelperDescriptorOne() } },
                    { assemblyTwo, new [] { provider.GetTagHelperDescriptorTwo() } },
                    { assemblyNone, Enumerable.Empty<TagHelperDescriptor>() }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AssemblyData))]
        public void CreateDescriptors_ReturnsCorrectDescriptors(
            string assemblyName,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var provider = new TestViewComponentDescriptorProvider();
            var factory = new ViewComponentTagHelperDescriptorFactory(provider);

            // Act
            var descriptors = factory.CreateDescriptors(assemblyName);

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.Default);
        }

        // Test invokes are needed for method creation in TestViewComponentDescriptorProvider.
        public enum TestEnum
        {
            A = 1,
            B = 2,
            C = 3
        }

        public void TestInvokeOne(string foo, string bar)
        {
        }

        public void TestInvokeTwo(TestEnum testEnum, string testString, int baz = 5)
        {
        }

        public void InvokeWithGenericParams(List<string> Foo, Dictionary<string, int> Bar)
        {
        }

        private class TestViewComponentDescriptorProvider : IViewComponentDescriptorProvider
        {
            private readonly ViewComponentDescriptor _viewComponentDescriptorOne = new ViewComponentDescriptor
            {
                DisplayName = "OneDisplayName",
                FullName = "OneViewComponent",
                ShortName = "One",
                MethodInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.TestInvokeOne)),
                TypeInfo = typeof(ViewComponentTagHelperDescriptorFactory).GetTypeInfo(),
                Parameters = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.TestInvokeOne)).GetParameters()
            };

            private readonly ViewComponentDescriptor _viewComponentDescriptorTwo = new ViewComponentDescriptor
            {
                DisplayName = "TwoDisplayName",
                FullName = "TwoViewComponent",
                ShortName = "Two",
                MethodInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.TestInvokeTwo)),
                TypeInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetTypeInfo(),
                Parameters = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.TestInvokeTwo)).GetParameters()
            };

            private readonly ViewComponentDescriptor _viewComponentDescriptorGeneric = new ViewComponentDescriptor
            {
                DisplayName = "GenericDisplayName",
                FullName = "GenericViewComponent",
                ShortName = "Generic",
                MethodInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.InvokeWithGenericParams)),
                TypeInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetTypeInfo(),
                Parameters = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.InvokeWithGenericParams)).GetParameters()
            };

            public TagHelperDescriptor GetTagHelperDescriptorOne()
            {
                var descriptor = new TagHelperDescriptor
                {
                    TagName = "vc:one",
                    TypeName = "__Generated__OneViewComponentTagHelper",
                    AssemblyName = "Microsoft.AspNetCore.Mvc.Razor",
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

                descriptor.PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "One");
                return descriptor;
            }

            public TagHelperDescriptor GetTagHelperDescriptorTwo()
            {
                var descriptor = new TagHelperDescriptor
                {
                    TagName = "vc:two",
                    TypeName = "__Generated__TwoViewComponentTagHelper",
                    AssemblyName = "Microsoft.AspNetCore.Mvc.Razor.Test",
                    Attributes = new List<TagHelperAttributeDescriptor>
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "test-enum",
                            PropertyName = "testEnum",
                            TypeName = typeof(TestEnum).FullName,
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

                descriptor.PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "Two");
                return descriptor;
            }

            public IEnumerable<ViewComponentDescriptor> GetViewComponents()
            {
                return new List<ViewComponentDescriptor>
                {
                    _viewComponentDescriptorOne,
                    _viewComponentDescriptorTwo,
                    _viewComponentDescriptorGeneric
                };
            }
        }
    }
}
