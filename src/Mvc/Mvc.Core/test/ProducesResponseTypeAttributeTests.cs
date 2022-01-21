// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc;

public class ProducesResponseTypeAttributeTests
{
    [Fact]
    public void ProducesResponseTypeAttribute_SetsContentType()
    {
        // Arrange
        var mediaType1 = new StringSegment("application/json");
        var mediaType2 = new StringSegment("text/json;charset=utf-8");
        var producesContentAttribute = new ProducesResponseTypeAttribute(typeof(void), StatusCodes.Status200OK, "application/json", "text/json;charset=utf-8");

        // Assert
        Assert.Equal(2, producesContentAttribute.ContentTypes.Count);
        MediaTypeAssert.Equal(mediaType1, producesContentAttribute.ContentTypes[0]);
        MediaTypeAssert.Equal(mediaType2, producesContentAttribute.ContentTypes[1]);
    }

    [Theory]
    [InlineData("application/*", "application/*")]
    [InlineData("application/xml, application/*, application/json", "application/*")]
    [InlineData("application/*, application/json", "application/*")]

    [InlineData("*/*", "*/*")]
    [InlineData("application/xml, */*, application/json", "*/*")]
    [InlineData("*/*, application/json", "*/*")]
    [InlineData("application/*+json", "application/*+json")]
    [InlineData("application/json;v=1;*", "application/json;v=1;*")]
    public void ProducesResponseTypeAttribute_InvalidContentType_Throws(string content, string invalidContentType)
    {
        // Act
        var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(
                   () => new ProducesResponseTypeAttribute(typeof(void), StatusCodes.Status200OK, contentTypes[0], contentTypes.Skip(1).ToArray()));

        Assert.Equal(
            $"Could not parse '{invalidContentType}'. Content types with wildcards are not supported.",
            ex.Message);
    }

    [Fact]
    public void ProducesResponseTypeAttribute_WithTypeOnly_SetsTypeProperty()
    {
        // Arrange
        var producesResponseTypeAttribute = new ProducesResponseTypeAttribute(typeof(Person), StatusCodes.Status200OK);

        // Act and Assert
        Assert.NotNull(producesResponseTypeAttribute.Type);
        Assert.Same(typeof(Person), producesResponseTypeAttribute.Type);
    }

    [Fact]
    public void ProducesResponseTypeAttribute_WithTypeOnly_DoesNotSetContentTypes()
    {
        // Arrange
        var producesResponseTypeAttribute = new ProducesResponseTypeAttribute(typeof(Person), StatusCodes.Status200OK);

        // Act and Assert
        Assert.Null(producesResponseTypeAttribute.ContentTypes);
    }

    private class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
