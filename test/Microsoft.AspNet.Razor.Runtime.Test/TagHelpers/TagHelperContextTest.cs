// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperContextTest
    {
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
                allAttributes: new Dictionary<string, object>(),
                items: expectedItems,
                uniqueId: string.Empty,
                getChildContentAsync: () => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            // Assert
            Assert.NotNull(context.Items);
            Assert.Same(expectedItems, context.Items);
            var item = Assert.Single(context.Items);
            Assert.Equal("test-entry", (string)item.Key, StringComparer.Ordinal);
            Assert.Equal(1234, item.Value);
        }
    }
}