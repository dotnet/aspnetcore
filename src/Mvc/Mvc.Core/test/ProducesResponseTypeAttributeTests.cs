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

    [Fact]
    public void ProducesResponseTypeAttribute_SetsDescription()
    {
        // Arrange
        var producesResponseTypeAttribute = new ProducesResponseTypeAttribute(typeof(Person), StatusCodes.Status200OK)
        {
            Description = "Example"
        };

        // Act and Assert
        Assert.Equal("Example", producesResponseTypeAttribute.Description);
    }

    [Fact]
    public void ProducesResponseTypeAttribute_WithTypeOnly_DoesNotSetDescription()
    {
        // Arrange
        var producesResponseTypeAttribute = new ProducesResponseTypeAttribute(typeof(Person), StatusCodes.Status200OK);

        // Act and Assert
        Assert.Null(producesResponseTypeAttribute.Description);
    }

    [Fact]
    public void ProducesResponseTypeAttribute_Generic_PreservesAllowMultipleAcrossInheritance()
    {
        // Arrange & Act
        var attributes = typeof(GenericDerivedClass).GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .Cast<ProducesResponseTypeAttribute>()
            .OrderBy(a => a.StatusCode)
            .ToArray();

        // Assert - all three attributes from the inheritance hierarchy should be present
        Assert.Equal(3, attributes.Length);
        Assert.Equal(500, attributes[0].StatusCode);
        Assert.Equal(401, attributes[1].StatusCode);
        Assert.Equal(403, attributes[2].StatusCode);
    }

    [ProducesResponseType<Person>(StatusCodes.Status500InternalServerError)]
    private class GenericBaseClass;

    [ProducesResponseType<Person>(StatusCodes.Status401Unauthorized)]
    private class GenericMiddleClass : GenericBaseClass;

    [ProducesResponseType<Person>(StatusCodes.Status403Forbidden)]
    private class GenericDerivedClass : GenericMiddleClass;

    private class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
