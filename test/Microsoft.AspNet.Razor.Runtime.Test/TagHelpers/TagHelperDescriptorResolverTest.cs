// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperDescriptorResolverTest : TagHelperTypeResolverTest
    {
        [Fact]
        public void DescriptorResolver_DoesNotReturnInvalidTagHelpersWhenSpecified()
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(TestableTagHelpers));

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_AbstractTagHelper, MyAssembly");
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_GenericTagHelper`, MyAssembly"));
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_NestedPublicTagHelper, MyAssembly"));
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_NestedInternalTagHelper, MyAssembly"));
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_PrivateTagHelper, MyAssembly"));
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_ProtectedTagHelper, MyAssembly"));
            descriptors = descriptors.Concat(tagHelperDescriptorResolver.Resolve(
                "Microsoft.AspNet.Razor.Runtime.Test.TagHelpers.Invalid_InternalTagHelper, MyAssembly"));

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
                OnGetLibraryDefinedTypes = (assemblyName) =>
                {
                    Assert.Equal("MyAssembly", assemblyName.Name);
                }
            };
            var tagHelperDescriptorResolver = new TagHelperDescriptorResolver(tagHelperTypeResolver);
            var expectedDescriptor = new TagHelperDescriptor("Valid_Plain",
                                                             typeof(Valid_PlainTagHelper).FullName,
                                                             ContentBehavior.None);

            // Act
            var descriptors = tagHelperDescriptorResolver.Resolve(lookupText);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(expectedDescriptor, descriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_ResolvesOnlyTypeResolverProvidedTypes()
        {
            // Arrange
            var resolver = new TagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "lookupText1", ValidTestableTagHelpers },
                        { "lookupText2", new Type[]{ typeof(Valid_PlainTagHelper) } }
                    }));
            var expectedDescriptor = new TagHelperDescriptor("Valid_Plain",
                                                             typeof(Valid_PlainTagHelper).FullName,
                                                             ContentBehavior.None);

            // Act
            var descriptors = resolver.Resolve("lookupText2");

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(descriptor, expectedDescriptor, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_ResolvesMultipleTypes()
        {
            // Arrange
            var resolver = new TagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "lookupText", new Type[]{ typeof(Valid_PlainTagHelper), typeof(Valid_InheritedTagHelper) } },
                    }));
            var expectedDescriptors = new TagHelperDescriptor[]
            {
                new TagHelperDescriptor("Valid_Plain",
                                        typeof(Valid_PlainTagHelper).FullName,
                                        ContentBehavior.None),
                new TagHelperDescriptor("Valid_Inherited",
                                        typeof(Valid_InheritedTagHelper).FullName,
                                        ContentBehavior.None)
            };

            // Act
            var descriptors = resolver.Resolve("lookupText").ToArray();

            // Assert
            Assert.Equal(descriptors.Length, 2);
            Assert.Equal(descriptors, expectedDescriptors, CompleteTagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void DescriptorResolver_DoesNotResolveTypesForNoTypeResolvingLookupText()
        {
            // Arrange
            var resolver = new TagHelperDescriptorResolver(
                new LookupBasedTagHelperTypeResolver(
                    new Dictionary<string, IEnumerable<Type>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "lookupText1", ValidTestableTagHelpers },
                        { "lookupText2", new Type[]{ typeof(Valid_PlainTagHelper) } }
                    }));

            // Act
            var descriptors = resolver.Resolve("lookupText").ToArray();

            // Assert
            Assert.Empty(descriptors);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void DescriptorResolver_ResolveThrowsIfNullOrEmptyLookupText(string lookupText)
        {
            // Arrange
            var tagHelperDescriptorResolver =
                new TagHelperDescriptorResolver(
                    new TestTagHelperTypeResolver(InvalidTestableTagHelpers));

            var expectedMessage =
                Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperLookupText(lookupText) +
                Environment.NewLine +
                "Parameter name: lookupText";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(nameof(lookupText),
            () =>
            {
                tagHelperDescriptorResolver.Resolve(lookupText);
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        private class LookupBasedTagHelperTypeResolver : TagHelperTypeResolver
        {
            private Dictionary<string, IEnumerable<Type>> _lookupValues;

            public LookupBasedTagHelperTypeResolver(Dictionary<string, IEnumerable<Type>> lookupValues)
            {
                _lookupValues = lookupValues;
            }

            internal override IEnumerable<TypeInfo> GetLibraryDefinedTypes(AssemblyName assemblyName)
            {
                IEnumerable<Type> types;

                _lookupValues.TryGetValue(assemblyName.Name, out types);

                return types?.Select(type => type.GetTypeInfo()) ?? Enumerable.Empty<TypeInfo>();
            }
        }
    }
}