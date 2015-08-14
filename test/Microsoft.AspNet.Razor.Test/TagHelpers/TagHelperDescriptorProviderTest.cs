// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperDescriptorProviderTest
    {
        public static TheoryData RequiredAttributeData
        {
            get
            {
                var divDescriptor = new TagHelperDescriptor
                {
                    TagName = "div",
                    TypeName = "DivTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "style" }
                };
                var inputDescriptor = new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "class", "style" }
                };
                var inputWildcardPrefixDescriptor = new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputWildCardAttribute",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "nodashprefix*" }
                };
                var catchAllDescriptor = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "class" }
                };
                var catchAllDescriptor2 = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "custom", "class" }
                };
                var catchAllWildcardPrefixDescriptor = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllWildCardAttribute",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { "prefix-*" }
                };
                var defaultAvailableDescriptors =
                    new[] { divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2 };
                var defaultWildcardDescriptors =
                    new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor };

                return new TheoryData<
                    string, // tagName
                    IEnumerable<string>, // providedAttributes
                    IEnumerable<TagHelperDescriptor>, // availableDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        "div",
                        new[] { "custom" },
                        defaultAvailableDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    { "div", new[] { "style" }, defaultAvailableDescriptors, new[] { divDescriptor } },
                    { "div", new[] { "class" }, defaultAvailableDescriptors, new[] { catchAllDescriptor } },
                    {
                        "div",
                        new[] { "class", "style" },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor }
                    },
                    {
                        "div",
                        new[] { "class", "style", "custom" },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor, catchAllDescriptor2 }
                    },
                    {
                        "input",
                        new[] { "class", "style" },
                        defaultAvailableDescriptors,
                        new[] { inputDescriptor, catchAllDescriptor }
                    },
                    {
                        "input",
                        new[] { "nodashprefixA" },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { "nodashprefix-ABC-DEF", "random" },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { "prefixABCnodashprefix" },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { "prefix-" },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { "nodashprefix" },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { "prefix-A" },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { "prefix-ABC-DEF", "random" },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { "prefix-abc", "nodashprefix-def" },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { "class", "prefix-abc", "onclick", "nodashprefix-def", "style" },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeData))]
        public void GetDescriptors_ReturnsDescriptorsWithRequiredAttributes(
            string tagName,
            IEnumerable<string> providedAttributes,
            IEnumerable<TagHelperDescriptor> availableDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(availableDescriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors(tagName, providedAttributes);

            // Assert
            Assert.Equal(expectedDescriptors, resolvedDescriptors, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_ReturnsEmptyDescriptorsWithPrefixAsTagName()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor(
                "th",
                TagHelperDescriptorProvider.ElementCatchAllTarget,
                "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors("th", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(resolvedDescriptors);
        }

        [Fact]
        public void GetDescriptors_OnlyUnderstandsSinglePrefix()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var spanDescriptor = CreatePrefixedDescriptor("th2:", "span", "foo2");
            var descriptors = new[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());
            var retrievedDescriptorsSpan = provider.GetDescriptors("th2:span", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(divDescriptor, descriptor);
            Assert.Empty(retrievedDescriptorsSpan);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllDescriptorsForPrefixedTags()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor("th:", TagHelperDescriptorProvider.ElementCatchAllTarget, "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());
            var retrievedDescriptorsSpan = provider.GetDescriptors("th:span", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(catchAllDescriptor, descriptor);
            descriptor = Assert.Single(retrievedDescriptorsSpan);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetDescriptors_ReturnsDescriptorsForPrefixedTags()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("div")]
        public void GetDescriptors_ReturnsNothingForUnprefixedTags(string tagName)
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", tagName, "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(retrievedDescriptorsDiv);
        }

        [Fact]
        public void GetDescriptors_ReturnsNothingForUnregisteredTags()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor
            {
                TagName = "div",
                TypeName = "foo1",
                AssemblyName = "SomeAssembly",
            };
            var spanDescriptor = new TagHelperDescriptor
            {
                TagName = "span",
                TypeName = "foo2",
                AssemblyName = "SomeAssembly",
            };
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("foo", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(retrievedDescriptors);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor
            {
                TagName = "div",
                TypeName = "foo1",
                AssemblyName = "SomeAssembly",
            };
            var spanDescriptor = new TagHelperDescriptor
            {
                TagName = "span",
                TypeName = "foo2",
                AssemblyName = "SomeAssembly",
            };
            var catchAllDescriptor = new TagHelperDescriptor
            {
                TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                TypeName = "foo3",
                AssemblyName = "SomeAssembly",
            };
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var divDescriptors = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());
            var spanDescriptors = provider.GetDescriptors("span", attributeNames: Enumerable.Empty<string>());

            // Assert
            // For divs
            Assert.Equal(2, divDescriptors.Count());
            Assert.Contains(divDescriptor, divDescriptors);
            Assert.Contains(catchAllDescriptor, divDescriptors);

            // For spans
            Assert.Equal(2, spanDescriptors.Count());
            Assert.Contains(spanDescriptor, spanDescriptors);
            Assert.Contains(catchAllDescriptor, spanDescriptors);
        }

        [Fact]
        public void GetDescriptors_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor
            {
                TagName = "div",
                TypeName = "foo1",
                AssemblyName = "SomeAssembly",
            };
            var descriptors = new TagHelperDescriptor[] { divDescriptor, divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        private static TagHelperDescriptor CreatePrefixedDescriptor(string prefix, string tagName, string typeName)
        {
            return new TagHelperDescriptor
            {
                Prefix = prefix,
                TagName = tagName,
                TypeName = typeName,
                AssemblyName = "SomeAssembly"
            };
        }
    }
}