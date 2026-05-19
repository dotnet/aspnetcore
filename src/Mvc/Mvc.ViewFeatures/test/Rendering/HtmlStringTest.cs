// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc.Rendering;

public class HtmlStringTest
{
    [Fact]
    public void WriteTo_WritesToTheSpecifiedWriter()
    {
        // Arrange
        var expectedText = "Some Text";
        var content = new HtmlString(expectedText);
        var writer = new StringWriter();

        // Act
        content.WriteTo(writer, new HtmlTestEncoder());

        // Assert
        Assert.Equal(expectedText, writer.ToString());
        writer.Dispose();
    }

    [Fact]
    public void FromEncodedText_DoesNotEncodeOnWrite()
    {
        // Arrange
        var expectedText = "Hello";

        // Act
        var content = new HtmlString(expectedText);

        // Assert
        Assert.Equal(expectedText, content.ToString());
    }

    [Fact]
    public void Empty_ReturnsEmptyString()
    {
        // Arrange & Act
        var content = HtmlString.Empty;

        // Assert
        Assert.Equal(string.Empty, content.ToString());
    }

    [Fact]
    public void ToString_ReturnsText()
    {
        // Arrange
        var expectedText = "Hello";
        var content = new HtmlString(expectedText);

        // Act
        var result = content.ToString();

        // Assert
        Assert.Equal(expectedText, result);
    }
}
