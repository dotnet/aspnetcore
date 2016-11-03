// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Host;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
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
                    { assemblyTwo, new [] { provider.GetTagHelperDescriptorTwo(), provider.GetTagHelperDescriptorGeneric() } },
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
            Assert.Equal(expectedDescriptors, descriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        public static TheoryData TypeData
        {
            get
            {
                var outParamType = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetMethod("MethodWithOutParam").GetParameters().First().ParameterType;
                var refParamType = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetMethod("MethodWithRefParam").GetParameters().First().ParameterType;

                return new TheoryData<Type, string>
                {
                    { typeof(string), "System.String" },
                    { typeof(string[,]), "System.String[,]" },
                    { typeof(List<int*[]>), "System.Collections.Generic.List<global::System.Int32*[]>" },
                    { typeof(List<string[,,]>), "System.Collections.Generic.List<global::System.String[,,]>" },
                    { typeof(Dictionary<string[], List<string>>),
                        "System.Collections.Generic.Dictionary<global::System.String[], global::System.Collections.Generic.List<global::System.String>>" },
                    { typeof(Dictionary<string, List<string[,]>>),
                        "System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.List<global::System.String[,]>>" },
                    { outParamType, "System.Collections.Generic.List<global::System.Char*[]>" },
                    { refParamType, "System.String[]" },
                    { typeof(NonGeneric.Nested1<bool, string>.Nested2),
                        "Microsoft.AspNetCore.Mvc.Razor.Test.Internal.ViewComponentTagHelperDescriptorFactoryTest.NonGeneric.Nested1<global::System.Boolean, global::System.String>.Nested2" },
                    { typeof(GenericType<string, int>.GenericNestedType<bool, string>),
                        "Microsoft.AspNetCore.Mvc.Razor.Test.Internal.ViewComponentTagHelperDescriptorFactoryTest.GenericType<global::System.String, global::System.Int32>.GenericNestedType<global::System.Boolean, global::System.String>" },
                    { typeof(GenericType<string, int>.NonGenericNested.MultiNestedType<bool, string>),
                        "Microsoft.AspNetCore.Mvc.Razor.Test.Internal.ViewComponentTagHelperDescriptorFactoryTest.GenericType<global::System.String, global::System.Int32>.NonGenericNested.MultiNestedType<global::System.Boolean, global::System.String>" },
                    { typeof(Dictionary<GenericType<string, int>.NonGenericNested.MultiNestedType<bool, string>, List<string[]>>),
                        "System.Collections.Generic.Dictionary<global::Microsoft.AspNetCore.Mvc.Razor.Test.Internal.ViewComponentTagHelperDescriptorFactoryTest.GenericType<global::System.String, global::System.Int32>.NonGenericNested.MultiNestedType<global::System.Boolean, global::System.String>, global::System.Collections.Generic.List<global::System.String[]>>" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TypeData))]
        public void GetCSharpTypeName_ReturnsCorrectTypeNames(Type type, string expected)
        {
            // Act
            var typeName = ViewComponentTagHelperDescriptorFactory.GetCSharpTypeName(type);

            // Assert
            Assert.Equal(expected, typeName);
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

        public void InvokeWithOpenGeneric<T>(List<T> baz)
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

            private readonly ViewComponentDescriptor _viewComponentDescriptorOpenGeneric = new ViewComponentDescriptor
            {
                DisplayName = "OpenGenericDisplayName",
                FullName = "OpenGenericViewComponent",
                ShortName = "OpenGeneric",
                MethodInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.InvokeWithOpenGeneric)),
                TypeInfo = typeof(ViewComponentTagHelperDescriptorFactoryTest).GetTypeInfo(),
                Parameters = typeof(ViewComponentTagHelperDescriptorFactoryTest)
                    .GetMethod(nameof(ViewComponentTagHelperDescriptorFactoryTest.InvokeWithOpenGeneric)).GetParameters()
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
                            TypeName = ViewComponentTagHelperDescriptorFactory.GetCSharpTypeName(typeof(TestEnum)),
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

            public TagHelperDescriptor GetTagHelperDescriptorGeneric()
            {
                var descriptor = new TagHelperDescriptor
                {
                    TagName = "vc:generic",
                    TypeName = "__Generated__GenericViewComponentTagHelper",
                    AssemblyName = "Microsoft.AspNetCore.Mvc.Razor.Test",
                    Attributes = new List<TagHelperAttributeDescriptor>
                    {
                        new TagHelperAttributeDescriptor
                        {
                            Name = "foo",
                            PropertyName = "Foo",
                            TypeName = ViewComponentTagHelperDescriptorFactory.GetCSharpTypeName(typeof(List<string>))
                        },

                        new TagHelperAttributeDescriptor
                        {
                            Name = "bar",
                            PropertyName = "Bar",
                            TypeName = ViewComponentTagHelperDescriptorFactory.GetCSharpTypeName(typeof(Dictionary<string, int>))
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

                descriptor.PropertyBag.Add(ViewComponentTagHelperDescriptorConventions.ViewComponentNameKey, "Generic");
                return descriptor;
            }

            public IEnumerable<ViewComponentDescriptor> GetViewComponents()
            {
                return new List<ViewComponentDescriptor>
                {
                    _viewComponentDescriptorOne,
                    _viewComponentDescriptorTwo,
                    _viewComponentDescriptorGeneric,
                    _viewComponentDescriptorOpenGeneric
                };
            }
        }

        public void MethodWithOutParam(out List<char*[]> foo)
        {
            foo = null;
        }

        public void MethodWithRefParam(ref string[] bar)
        {
        }

        private class GenericType<T1, T2>
        {
            public class GenericNestedType<T3, T4>
            {
            }

            public class NonGenericNested
            {
                public class MultiNestedType<T5, T6>
                {
                }
            }
        }

        private class NonGeneric
        {
            public class Nested1<T1, T2>
            {
                public class Nested2
                {

                }
            }
        }
    }
}
