// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Compilation.TagHelpers
{
    public class TagHelperDescriptorProviderTest
    {
        public static TheoryData RequiredParentData
        {
            get
            {
                var strongPParent = new TagHelperDescriptor
                {
                    TagName = "strong",
                    TypeName = "StrongTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredParent = "p",
                };
                var strongDivParent = new TagHelperDescriptor
                {
                    TagName = "strong",
                    TypeName = "StrongTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredParent = "div",
                };
                var catchAllPParent = new TagHelperDescriptor
                {
                    TagName = "*",
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredParent = "p",
                };

                return new TheoryData<
                    string, // tagName
                    string, // parentTagName
                    IEnumerable<TagHelperDescriptor>, // availableDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        "strong",
                        "p",
                        new[] { strongPParent, strongDivParent },
                        new[] { strongPParent }
                    },
                    {
                        "strong",
                        "div",
                        new[] { strongPParent, strongDivParent, catchAllPParent },
                        new[] { strongDivParent }
                    },
                    {
                        "strong",
                        "p",
                        new[] { strongPParent, strongDivParent, catchAllPParent },
                        new[] { strongPParent, catchAllPParent }
                    },
                    {
                        "custom",
                        "p",
                        new[] { strongPParent, strongDivParent, catchAllPParent },
                        new[] { catchAllPParent }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredParentData))]
        public void GetDescriptors_ReturnsDescriptorsParentTags(
            string tagName,
            string parentTagName,
            IEnumerable<TagHelperDescriptor> availableDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(availableDescriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors(
                tagName,
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: parentTagName);

            // Assert
            Assert.Equal(expectedDescriptors, resolvedDescriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
        }

        public static TheoryData RequiredAttributeData
        {
            get
            {
                var divDescriptor = new TagHelperDescriptor
                {
                    TagName = "div",
                    TypeName = "DivTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "style" } }
                };
                var inputDescriptor = new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[]
                    {
                        new TagHelperRequiredAttributeDescriptor { Name = "class" },
                        new TagHelperRequiredAttributeDescriptor { Name = "style" }
                    }
                };
                var inputWildcardPrefixDescriptor = new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputWildCardAttribute",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[]
                    {
                        new TagHelperRequiredAttributeDescriptor
                        {
                            Name = "nodashprefix",
                            NameComparison = TagHelperRequiredAttributeNameComparison.PrefixMatch,
                        }
                    }
                };
                var catchAllDescriptor = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "class" } }
                };
                var catchAllDescriptor2 = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllTagHelper",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[]
                    {
                        new TagHelperRequiredAttributeDescriptor { Name = "custom" },
                        new TagHelperRequiredAttributeDescriptor { Name = "class" }
                    }
                };
                var catchAllWildcardPrefixDescriptor = new TagHelperDescriptor
                {
                    TagName = TagHelperDescriptorProvider.ElementCatchAllTarget,
                    TypeName = "CatchAllWildCardAttribute",
                    AssemblyName = "SomeAssembly",
                    RequiredAttributes = new[]
                    {
                        new TagHelperRequiredAttributeDescriptor
                        {
                            Name = "prefix-",
                            NameComparison = TagHelperRequiredAttributeNameComparison.PrefixMatch,
                        }
                    }
                };
                var defaultAvailableDescriptors =
                    new[] { divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2 };
                var defaultWildcardDescriptors =
                    new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor };
                Func<string, KeyValuePair<string, string>> kvp =
                    (name) => new KeyValuePair<string, string>(name, "test value");

                return new TheoryData<
                    string, // tagName
                    IEnumerable<KeyValuePair<string, string>>, // providedAttributes
                    IEnumerable<TagHelperDescriptor>, // availableDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        "div",
                        new[] { kvp("custom") },
                        defaultAvailableDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    { "div", new[] { kvp("style") }, defaultAvailableDescriptors, new[] { divDescriptor } },
                    { "div", new[] { kvp("class") }, defaultAvailableDescriptors, new[] { catchAllDescriptor } },
                    {
                        "div",
                        new[] { kvp("class"), kvp("style") },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor }
                    },
                    {
                        "div",
                        new[] { kvp("class"), kvp("style"), kvp("custom") },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor, catchAllDescriptor2 }
                    },
                    {
                        "input",
                        new[] { kvp("class"), kvp("style") },
                        defaultAvailableDescriptors,
                        new[] { inputDescriptor, catchAllDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefixA") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefix-ABC-DEF"), kvp("random") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefixABCnodashprefix") },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { kvp("prefix-") },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefix") },
                        defaultWildcardDescriptors,
                        Enumerable.Empty<TagHelperDescriptor>()
                    },
                    {
                        "input",
                        new[] { kvp("prefix-A") },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefix-ABC-DEF"), kvp("random") },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefix-abc"), kvp("nodashprefix-def") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("class"), kvp("prefix-abc"), kvp("onclick"), kvp("nodashprefix-def"), kvp("style") },
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
            IEnumerable<KeyValuePair<string, string>> providedAttributes,
            IEnumerable<TagHelperDescriptor> availableDescriptors,
            IEnumerable<TagHelperDescriptor> expectedDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(availableDescriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors(tagName, providedAttributes, parentTagName: "p");

            // Assert
            Assert.Equal(expectedDescriptors, resolvedDescriptors, CaseSensitiveTagHelperDescriptorComparer.Default);
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
            var resolvedDescriptors = provider.GetDescriptors(
                tagName: "th",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptorsDiv = provider.GetDescriptors(
                tagName: "th:div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var retrievedDescriptorsSpan = provider.GetDescriptors(
                tagName: "th2:span",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptorsDiv = provider.GetDescriptors(
                tagName: "th:div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var retrievedDescriptorsSpan = provider.GetDescriptors(
                tagName: "th:span",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptors = provider.GetDescriptors(
                tagName: "th:div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptorsDiv = provider.GetDescriptors(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptors = provider.GetDescriptors(
                tagName: "foo",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var divDescriptors = provider.GetDescriptors(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var spanDescriptors = provider.GetDescriptors(
                tagName: "span",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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
            var retrievedDescriptors = provider.GetDescriptors(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

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