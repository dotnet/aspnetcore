// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.TagHelpers;

public class TagHelperContextTest
{
    [Fact]
    public void Reinitialize_AllowsContextToBeReused()
    {
        // Arrange
        var tagName = "test";
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
        var context = new TagHelperContext(
            tagName,
            initialAttributes,
            initialItems,
            initialUniqueId);

        // Act
        context.Reinitialize(tagName, expectedItems, expectedUniqueId);

        // Assert
        Assert.Equal(tagName, context.TagName);
        Assert.Same(expectedItems, context.Items);
        Assert.Equal(expectedUniqueId, context.UniqueId);
        Assert.Empty(context.AllAttributes);
    }

    [Fact]
    public void Constructor_SetsProperties_AsExpected_WithTagName()
    {
        // Arrange
        var expectedItems = new Dictionary<object, object>
            {
                { "test-entry", 1234 }
            };

        // Act
        var context = new TagHelperContext(
            tagName: "test",
            allAttributes: new TagHelperAttributeList(),
            items: expectedItems,
            uniqueId: string.Empty);

        // Assert
        Assert.Equal("test", context.TagName);
        Assert.NotNull(context.Items);
        Assert.Same(expectedItems, context.Items);
        var item = Assert.Single(context.Items);
        Assert.Equal("test-entry", (string)item.Key, StringComparer.Ordinal);
        Assert.Equal(1234, item.Value);
    }

    [Fact]
    public void Constructor_SetsProperties_AsExpected_WithoutTagName()
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
