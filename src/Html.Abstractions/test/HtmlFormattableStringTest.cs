// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Html;

public class HtmlFormattableStringTest
{
    [Fact]
    public void HtmlFormattableString_EmptyArgs()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("Hello, World!");

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void HtmlFormattableString_EmptyArgsAndCulture()
    {
        // Arrange
        var formattableString = new HtmlFormattableString(CultureInfo.CurrentCulture, "Hello, World!");

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void HtmlFormattableString_MultipleArguments()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("{0} {1} {2} {3}!", "First", "Second", "Third", "Fourth");

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal(
            "HtmlEncode[[First]] HtmlEncode[[Second]] HtmlEncode[[Third]] HtmlEncode[[Fourth]]!",
            result);
    }

    [Fact]
    public void HtmlFormattableString_WithHtmlString()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("{0}!", new HtmlString("First"));

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("First!", result);
    }

    [Fact]
    public void HtmlFormattableString_WithOtherIHtmlContent()
    {
        // Arrange
        var builder = new HtmlContentBuilder();
        builder.Append("First");

        var formattableString = new HtmlFormattableString("{0}!", builder);

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("HtmlEncode[[First]]!", result);
    }

    // This test is needed to ensure the shared StringWriter gets cleared.
    [Fact]
    public void HtmlFormattableString_WithMultipleHtmlContentArguments()
    {
        // Arrange
        var formattableString = new HtmlFormattableString(
            "Happy {0}, {1}!",
            new HtmlString("Birthday"),
            new HtmlContentBuilder().Append("Billy"));

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("Happy Birthday, HtmlEncode[[Billy]]!", result);
    }

    [Fact]
    public void HtmlFormattableString_WithHtmlString_AndOffset()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("{0, 20}!", new HtmlString("First"));

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("               First!", result);
    }

    [Fact]
    public void HtmlFormattableString_With3Arguments()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("0x{0:X} - {1} equivalent for {2}.", 50, "hex", 50);

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal(
            "0xHtmlEncode[[32]] - HtmlEncode[[hex]] equivalent for HtmlEncode[[50]].",
            result);
    }

    [Fact]
    public void HtmlFormattableString_WithAlignmentComponent()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("{0, -25} World!", "Hello");

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal(
            "HtmlEncode[[Hello]]       World!", result);
    }

    [Fact]
    public void HtmlFormattableString_WithFormatStringComponent()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("0x{0:X}", 50);

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("0xHtmlEncode[[32]]", result);
    }

    [Fact]
    public void HtmlFormattableString_WithCulture()
    {
        // Arrange
        var formattableString = new HtmlFormattableString(
            CultureInfo.InvariantCulture,
            "Numbers in InvariantCulture - {0, -5:N} {1} {2} {3}!",
            1.1,
            2.98,
            145.82,
            32.86);

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal(
            "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]] " +
                "HtmlEncode[[145.82]] HtmlEncode[[32.86]]!",
            result);
    }

    [Fact]
    [ReplaceCulture("en-US", "en-US")]
    public void HtmlFormattableString_UsesPassedInCulture()
    {
        // Arrange
        var culture = new CultureInfo("fr-FR");
        var formattableString = new HtmlFormattableString(culture, "{0} in french!", 1.21);

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("HtmlEncode[[1,21]] in french!", result);
    }

    [Fact]
    [ReplaceCulture("de-DE", "de-DE")]
    public void HtmlFormattableString_UsesCurrentCulture()
    {
        // Arrange
        var formattableString = new HtmlFormattableString("{0:D}", new DateTime(2015, 02, 01));

        // Act
        var result = HtmlContentToString(formattableString);

        // Assert
        Assert.Equal("HtmlEncode[[Sonntag, 1. Februar 2015]]", result);
    }

    private static string HtmlContentToString(IHtmlContent content)
    {
        using (var writer = new StringWriter())
        {
            content.WriteTo(writer, new HtmlTestEncoder());
            return writer.ToString();
        }
    }
}
