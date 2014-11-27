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
        public void TagHelperDescriptorProvider_GetTagHelpersReturnsNothingForUnregisteredTags()
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
        public void TagHelperDescriptorProvider_GetTagHelpersDoesNotReturnNonCatchAllTagsForCatchAll()
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
        public void TagHelperDescriptorProvider_GetTagHelpersReturnsCatchAllsWithEveryTagName()
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
        public void TagHelperDescriptorProvider_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
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
    }
}