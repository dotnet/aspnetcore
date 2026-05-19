// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

public class MetadataTest
{
    [Fact]
    public void EndpointDescriptionAttribute_ToString()
    {
        // Arrange
        var metadata = new EndpointDescriptionAttribute("A description");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Description: A description", value);
    }

    [Fact]
    public void EndpointSummaryAttribute_ToString()
    {
        // Arrange
        var metadata = new EndpointSummaryAttribute("A summary");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Summary: A summary", value);
    }

    [Fact]
    public void HostAttribute_ToString()
    {
        // Arrange
        var metadata = new TagsAttribute("Tag1", "Tag2");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Tags: Tag1,Tag2", value);
    }
}
