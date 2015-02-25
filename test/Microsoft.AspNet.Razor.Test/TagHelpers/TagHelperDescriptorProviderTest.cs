// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperDescriptorProviderTest
    {
        [Fact]
        public void GetTagHelpers_ReturnsEmptyDescriptorsWithPrefixAsTagName()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor("th", "*", "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var resolvedDescriptors = provider.GetTagHelpers("th");

            // Assert
            Assert.Empty(resolvedDescriptors);
        }

        [Fact]
        public void GetTagHelpers_OnlyUnderstandsSinglePrefix()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var spanDescriptor = CreatePrefixedDescriptor("th2:", "span", "foo2");
            var descriptors = new[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetTagHelpers("th:div");
            var retrievedDescriptorsSpan = provider.GetTagHelpers("th2:span");

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(divDescriptor, descriptor);
            Assert.Empty(retrievedDescriptorsSpan);
        }

        [Fact]
        public void GetTagHelpers_ReturnsCatchAllDescriptorsForPrefixedTags()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor("th:", "*", "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetTagHelpers("th:div");
            var retrievedDescriptorsSpan = provider.GetTagHelpers("th:span");

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(catchAllDescriptor, descriptor);
            descriptor = Assert.Single(retrievedDescriptorsSpan);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetTagHelpers_ReturnsDescriptorsForPrefixedTags()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetTagHelpers("th:div");

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("div")]
        public void GetTagHelpers_ReturnsNothingForUnprefixedTags(string tagName)
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", tagName, "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetTagHelpers("div");

            // Assert
            Assert.Empty(retrievedDescriptorsDiv);
        }

        [Fact]
        public void GetTagHelpers_ReturnsNothingForUnregisteredTags()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetTagHelpers("foo");

            // Assert
            Assert.Empty(retrievedDescriptors);
        }

        [Fact]
        public void GetTagHelpers_DoesNotReturnNonCatchAllTagsForCatchAll()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var catchAllDescriptor = new TagHelperDescriptor("*", "foo3", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetTagHelpers("*");

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetTagHelpers_ReturnsCatchAllsWithEveryTagName()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var catchAllDescriptor = new TagHelperDescriptor("*", "foo3", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var divDescriptors = provider.GetTagHelpers("div");
            var spanDescriptors = provider.GetTagHelpers("span");

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
        public void GetTagHelpers_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetTagHelpers("div");

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        private static TagHelperDescriptor CreatePrefixedDescriptor(string prefix, string tagName, string typeName)
        {
            return new TagHelperDescriptor(
                prefix, 
                tagName, 
                typeName, 
                assemblyName: "SomeAssembly", 
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>());
        }
    }
}