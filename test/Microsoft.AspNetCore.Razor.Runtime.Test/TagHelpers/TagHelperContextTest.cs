// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class TagHelperContextTest
    {
        [Fact]
        public void Reinitialize_AllowsContextToBeReused()
        {
            // Arrange
            var initialUniqueId = "123";
            var expectedUniqueId = "456";
            var initialItems = new Dictionary<object, object>
            {
                { "test-entry", 1234 }
            };
            var expectedItems = new Dictionary<object, object>
            {
                { "something", "new" }
            };
            var initialAttributes = new TagHelperAttributeList
            {
                { "name", "value" }
            };
            var context = new TagHelperContext(initialAttributes, initialItems, initialUniqueId);

            // Act
            context.Reinitialize(expectedItems, expectedUniqueId);

            // Assert
            Assert.Same(expectedItems, context.Items);
            Assert.Equal(expectedUniqueId, context.UniqueId);
            Assert.Empty(context.AllAttributes);
        }

        [Fact]
        public void Constructor_SetsProperties_AsExpected()
        {
            // Arrange
            var expectedItems = new Dictionary<object, object>
            {
                { "test-entry", 1234 }
            };

            // Act
            var context = new TagHelperContext(
                allAttributes: new TagHelperAttributeList(),
                items: expectedItems,
                uniqueId: string.Empty);

            // Assert
            Assert.NotNull(context.Items);
            Assert.Same(expectedItems, context.Items);
            var item = Assert.Single(context.Items);
            Assert.Equal("test-entry", (string)item.Key, StringComparer.Ordinal);
            Assert.Equal(1234, item.Value);
        }
    }
}