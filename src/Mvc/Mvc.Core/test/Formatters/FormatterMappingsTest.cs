// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class FormatterMappingsTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FormatterMappings_GetMediaTypeMappingForFormat_ThrowsForInvalidFormats(string format)
    {
        // Arrange
        var options = new FormatterMappings();

        // Act & Assert
        Assert.Throws<ArgumentException>("format", () => options.GetMediaTypeMappingForFormat(format));
    }

    [Theory]
    [InlineData(".xml", "application/xml", "xml")]
    [InlineData("json", "application/json", "JSON")]
    [InlineData(".foo", "text/foo", "Foo")]
    [InlineData(".Json", "application/json", "json")]
    [InlineData("FOo", "text/foo", "FOO")]
    public void FormatterMappings_SetFormatMapping_DiffSetGetFormat(string setFormat, string contentType, string getFormat)
    {
        // Arrange
        var options = new FormatterMappings();
        options.SetMediaTypeMappingForFormat(setFormat, MediaTypeHeaderValue.Parse(contentType));

        // Act
        var returnMediaType = options.GetMediaTypeMappingForFormat(getFormat);

        // Assert
        MediaTypeAssert.Equal(contentType, returnMediaType);
    }

    [Fact]
    public void FormatterMappings_Invalid_Period()
    {
        // Arrange
        var options = new FormatterMappings();
        var format = ".";
        var expected = $"The format provided is invalid '{format}'. A format must be a non-empty file-" +
            "extension, optionally prefixed with a '.' character.";

        // Act and assert
        ExceptionAssert.ThrowsArgument(
            () => options.SetMediaTypeMappingForFormat(
                format,
                MediaTypeHeaderValue.Parse("application/xml")),
            "format",
            expected);
    }

    [Theory]
    [InlineData(null, "Value cannot be null.")]
    [InlineData("", "The value cannot be an empty string.")]
    public void FormatterMappings_SetFormatMapping_FormatEmpty(string format, string expectedMessage)
    {
        // Arrange
        var options = new FormatterMappings();

        // Act and Assert
        ExceptionAssert.ThrowsArgument(
            () => options.SetMediaTypeMappingForFormat(
                format,
                MediaTypeHeaderValue.Parse("application/xml")),
            "format",
            expectedMessage);
    }

    [Theory]
    [InlineData("application/*")]
    [InlineData("*/json")]
    [InlineData("*/*")]
    public void FormatterMappings_WildcardFormat(string format)
    {
        // Arrange
        var options = new FormatterMappings();
        var expected = $@"The media type ""{format}"" is not valid. MediaTypes containing wildcards (*) " +
            "are not allowed in formatter mappings.";

        // Act and assert
        ExceptionAssert.ThrowsArgument(
            () => options.SetMediaTypeMappingForFormat(
                "star",
                MediaTypeHeaderValue.Parse(format)),
            "contentType",
            expected);
    }

    [Theory]
    [InlineData(".xml", true)]
    [InlineData("json", true)]
    [InlineData(".foo", true)]
    [InlineData(".Json", true)]
    [InlineData("FOo", true)]
    [InlineData("bar", true)]
    [InlineData("baz", false)]
    [InlineData(".baz", false)]
    [InlineData("BAZ", false)]
    public void FormatterMappings_ClearFormatMapping(string format, bool expected)
    {
        // Arrange
        var options = new FormatterMappings();
        var mediaType = MediaTypeHeaderValue.Parse("application/xml");
        options.SetMediaTypeMappingForFormat("xml", mediaType);
        mediaType = MediaTypeHeaderValue.Parse("application/json");
        options.SetMediaTypeMappingForFormat("json", mediaType);
        mediaType = MediaTypeHeaderValue.Parse("application/foo");
        options.SetMediaTypeMappingForFormat("foo", mediaType);
        mediaType = MediaTypeHeaderValue.Parse("application/bar");
        options.SetMediaTypeMappingForFormat("bar", mediaType);

        // Act
        var cleared = options.ClearMediaTypeMappingForFormat(format);

        // Assert
        Assert.Equal(expected, cleared);
        Assert.Null(options.GetMediaTypeMappingForFormat(format));
    }
}
