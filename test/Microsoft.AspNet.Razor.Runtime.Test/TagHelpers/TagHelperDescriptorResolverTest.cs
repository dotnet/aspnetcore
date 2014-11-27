// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorResolverTest : TagHelperTypeResolverTest
    {
        private static readonly string AssemblyName =
            typeof(TagHelperDescriptorFactoryTest).GetTypeInfo().Assembly.GetName().Name;

        private static readonly Type Valid_PlainTagHelperType = typeof(Valid_PlainTagHelper);

        private static readonly Type Valid_InheritedTagHelperType = typeof(Valid_InheritedTagHelper);

        private static TagHelperDescriptor Valid_PlainTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor("valid_plain",
                                               Valid_PlainTagHelperType.FullName,
                                               AssemblyName);
            }
        }

        private static TagHelperDescriptor Valid_InheritedTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor("valid_inherited",
                                               Valid_InheritedTagHelperType.FullName,
                                               AssemblyName);
            }
        }

        [Theory]
        [InlineData("MyType, MyAssembly", "MyAssembly")]
        [InlineData("MyAssembly2", "MyAssembly2")]
        public void Resolve_AllowsOverridenResolveDescriptorsInAssembly(string lookupText, string expectedAssemblyName)
        {
            // Arrange
            var tagHelperDescriptorResolver = new AssemblyCheckingTagHelperDescriptorResolver();
            var context = new TagHelperDescriptorResolutionContext(
                new[] { new TagHelperDirectiveDescriptor(lookupText, TagHelperDirectiveType.AddTagHelper) },
                new ParserErrorSink());

            // Act
            tagHelperDescriptorResolver.Resolve(context);

            // Assert
            Assert.Equal(expectedAssemblyName, tagHelperDescriptorResolver.CalledWithAssemblyName);
        }

        public static TheoryData ResolveDirectiveDescriptorsData
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);

                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;

                // We're treating 'string' as a TagHelper so we can test TagHelpers in multiple assemblies without
                // building a separate assembly with a single TagHelper.
                var stringTagHelperDescriptor =
                    new TagHelperDescriptor("string",
                                            "System.String",
                                            assemblyB);

                return new TheoryData<Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                                      IEnumerable<TagHelperDirectiveDescriptor>, // directiveDescriptors
                                      IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor, stringTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { stringTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_PlainTagHelperDescriptor, Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } }
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.RemoveTagHelper)
                        },
                        new [] { Valid_InheritedTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(
                                Valid_PlainTagHelperType.FullName + ", " + assemblyA,
                                TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper)
                        },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                        },
                        new [] { Valid_InheritedTagHelperDescriptor, Valid_PlainTagHelperDescriptor }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsData))]
        public void Resolve_ReturnsDescriptorsBasedOnDirectiveDescriptors(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors, 
                new ParserErrorSink());

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Equal(expectedDescriptors.Count(), descriptors.Count());

            foreach (var expectedDescriptor in expectedDescriptors)
            {
                Assert.Contains(expectedDescriptor, descriptors, TagHelperDescriptorComparer.Default);
            }
        }

        public static TheoryData ResolveDirectiveDescriptorsData_EmptyResult
        {
            get
            {
                var assemblyA = AssemblyName;
                var stringType = typeof(string);

                var assemblyB = stringType.GetTypeInfo().Assembly.GetName().Name;
                var stringTagHelperDescriptor =
                    new TagHelperDescriptor("string",
                                            "System.String",
                                            assemblyB);

                return new TheoryData<Dictionary<string, IEnumerable<Type>>, // descriptorAssemblyLookups
                                      IEnumerable<TagHelperDirectiveDescriptor>> // directiveDescriptors
                {
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(Valid_InheritedTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>
                        {
                            { assemblyA, new [] { Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                            { assemblyB, new [] { stringType } },
                        },
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(assemblyB, TagHelperDirectiveType.AddTagHelper),
                            new TagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(Valid_InheritedTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(stringType.FullName + ", " + assemblyB, TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new Dictionary<string, IEnumerable<Type>>(),
                        new []
                        {
                            new TagHelperDirectiveDescriptor(assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                            new TagHelperDirectiveDescriptor(Valid_PlainTagHelperType.FullName + ", " + assemblyA, TagHelperDirectiveType.RemoveTagHelper),
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveDirectiveDescriptorsData_EmptyResult))]
        public void Resolve_CanReturnEmptyDescriptorsBasedOnDirectiveDescriptors(
            Dictionary<string, IEnumerable<Type>> descriptorAssemblyLookups,
            IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new LookupBasedTagHelperTypeResolver(descriptorAssemblyLookups));
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                directiveDescriptors,
                new ParserErrorSink());

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DescriptorResolver_DoesNotReturnInvalidTagHelpersWhenSpecified()
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(TestableTagHelpers));

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_AbstractTagHelper, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_GenericTagHelper`, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_NestedPublicTagHelper, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_NestedInternalTagHelper, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_PrivateTagHelper, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_ProtectedTagHelper, " + AssemblyName,
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_InternalTagHelper, " + AssemblyName);

            // Assert
            Assert.Empty(descriptors);
        }

        [Theory]
        [InlineData("Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,MyAssembly")]
        [InlineData("    Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,MyAssembly")]
        [InlineData("Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper    ,MyAssembly")]
        [InlineData("    Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper    ,MyAssembly")]
        [InlineData("Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,    MyAssembly")]
        [InlineData("Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,MyAssembly    ")]
        [InlineData("Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,    MyAssembly    ")]
        [InlineData("    Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper,    MyAssembly    ")]
        [InlineData("    Microsoft.AspNet.Razor.Runtime.TagHelpers.Valid_PlainTagHelper    ,    MyAssembly    ")]
        public void DescriptorResolver_IgnoresSpaces(string lookupText)
        {
            // Arrange
            var tagHelperTypeResolver = new TestTagHelperTypeResolver(TestableTagHelpers)
            {
                OnGetExportedTypes = (assemblyName) =>
                {
                    Assert.Equal("MyAssembly", assemblyName.Name);
                }
            };
            var tagHelperDescriptorResolver = new TestTagHelperDescriptorResolver(tagHelperTypeResolver);

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(lookupText);

            // Assert
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DescriptorResolver_ResolvesOnlyTypeResolverProvidedTypes()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, ValidTestableTagHelpers },
                        {
                            Valid_PlainTagHelperType.FullName + ", " + AssemblyName,
                            new Type[] { Valid_PlainTagHelperType }
                        }
                    }));

            // Act
            var descriptors = resolver.Resolve(Valid_PlainTagHelperType + ", " + AssemblyName);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(Valid_PlainTagHelperDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_ResolvesMultipleTypes()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, new Type[]{ Valid_PlainTagHelperType, Valid_InheritedTagHelperType } },
                    }));
            var expectedDescriptors = new TagHelperDescriptor[]
            {
                Valid_PlainTagHelperDescriptor,
                Valid_InheritedTagHelperDescriptor
            };

            // Act
            var descriptors = resolver.Resolve(AssemblyName).ToArray();

            // Assert
            Assert.Equal(descriptors.Length, 2);
            Assert.Equal(expectedDescriptors, descriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_DoesNotResolveTypesForNoTypeResolvingLookupText()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { AssemblyName, ValidTestableTagHelpers },
                        {
                            Valid_PlainTagHelperType.FullName + ", " + AssemblyName,
                            new Type[]{ Valid_PlainTagHelperType }
                        }
                    }));

            // Act
            var descriptors = resolver.Resolve("lookupText").ToArray();

            // Assert
            Assert.Empty(descriptors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void DescriptorResolver_CreatesErrorIfNullOrEmptyLookupText_DoesNotThrow(string lookupText)
        {
            // Arrange
            var errorSink = new ParserErrorSink();
            var tagHelperDescriptorResolver =
                new TestTagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(InvalidTestableTagHelpers));
            var documentLocation = new SourceLocation(1, 2, 3);
            var directiveType = TagHelperDirectiveType.AddTagHelper;
            var expectedErrorMessage =
                Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperLookupText(lookupText);
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new [] { new TagHelperDirectiveDescriptor(lookupText, documentLocation, directiveType)},
                errorSink);

            // Act
            tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(1, error.Length);
            Assert.Equal(documentLocation, error.Location);
            Assert.Equal(expectedErrorMessage, error.Message);
        }

        [Fact]
        public void DescriptorResolver_UnderstandsUnexpectedExceptions_DoesNotThrow()
        {
            // Arrange
            var expectedErrorMessage = "Encountered an unexpected error when attempting to resolve tag helper " +
                                       "directive '@addtaghelper' with value 'A custom lookup text'. Error: A " +
                                       "custom exception";
            var documentLocation = new SourceLocation(1, 2, 3);
            var directiveType = TagHelperDirectiveType.AddTagHelper;
            var errorSink = new ParserErrorSink();
            var expectedError = new Exception("A custom exception");
            var tagHelperDescriptorResolver = new ThrowingTagHelperDescriptorResolver(expectedError);
            var resolutionContext = new TagHelperDescriptorResolutionContext(
                new[] { new TagHelperDirectiveDescriptor("A custom lookup text", documentLocation, directiveType) },
                errorSink);


            // Act
            tagHelperDescriptorResolver.Resolve(resolutionContext);

            // Assert
            var error = Assert.Single(errorSink.Errors);
            Assert.Equal(1, error.Length);
            Assert.Equal(documentLocation, error.Location);
            Assert.Equal(expectedErrorMessage, error.Message);
        }

        private class TestTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            public TestTagHelperDescriptorResolver(TagHelperTypeResolver typeResolver)
                : base(typeResolver)
            {
            }

            public IEnumerable<TagHelperDescriptor> Resolve(params string[] lookupTexts)
            {
                return Resolve(
                    new TagHelperDescriptorResolutionContext(
                        lookupTexts.Select(
                            lookupText =>
                                new TagHelperDirectiveDescriptor(lookupText, TagHelperDirectiveType.AddTagHelper)),
                    new ParserErrorSink()));
            }
        }

        private class LookupBasedTagHelperTypeResolver : TagHelperTypeResolver
        {
            private Dictionary<string, IEnumerable<Type>> _lookupValues;

            public LookupBasedTagHelperTypeResolver(Dictionary<string, IEnumerable<Type>> lookupValues)
            {
                _lookupValues = lookupValues;
            }

            protected override IEnumerable<TypeInfo> GetExportedTypes(AssemblyName assemblyName)
            {
                IEnumerable<Type> types;

                _lookupValues.TryGetValue(assemblyName.Name, out types);

                return types?.Select(type => type.GetTypeInfo()) ?? Enumerable.Empty<TypeInfo>();
            }

            internal override bool IsTagHelper(TypeInfo typeInfo)
            {
                return true;
            }
        }

        private class AssemblyCheckingTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            public string CalledWithAssemblyName { get; set; }

            protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
                string assemblyName,
                SourceLocation documentLocation,
                ParserErrorSink errorSink)
            {
                CalledWithAssemblyName = assemblyName;

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }

        private class ThrowingTagHelperDescriptorResolver : TagHelperDescriptorResolver
        {
            private readonly Exception _error;

            public ThrowingTagHelperDescriptorResolver(Exception error)
            {
                _error = error;
            }

            protected override IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
                string assemblyName, 
                SourceLocation documentLocation, 
                ParserErrorSink errorSink)
            {
                throw _error;
            }
        }
    }
}