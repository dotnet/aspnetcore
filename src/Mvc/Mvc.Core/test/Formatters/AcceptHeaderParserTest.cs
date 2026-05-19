// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class AcceptHeaderParserTest
{
    [Fact]
    public void ParseAcceptHeader_ParsesSimpleHeader()
    {
        // Arrange
        var header = "application/json";
        var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0)
            };

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string> { header });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_ParsesSimpleHeaderWithMultipleValues()
    {
        // Arrange
        var header = "application/json, application/xml;q=0.8";
        var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/xml;q=0.8"),0.8)
            };

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string> { header });

        // Assert
        Assert.Equal(expected, parsed);
        foreach (var mediaType in parsed)
        {
            Assert.Same(header, mediaType.MediaType.Buffer);
        }
    }

    [Fact]
    public void ParseAcceptHeader_ParsesSimpleHeaderWithMultipleValues_InvalidFormat()
    {
        // Arrange
        var header = "application/json, application/xml,;q=0.8";
        var expectedMediaTypes = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/xml"),1.0),
            };

        // Act
        var mediaTypes = AcceptHeaderParser.ParseAcceptHeader(new List<string> { header });

        // Assert
        Assert.Equal(expectedMediaTypes, mediaTypes);
    }

    public static TheoryData<string[], string[]> ParseAcceptHeaderWithInvalidMediaTypesData =>
        new TheoryData<string[], string[]>
        {
                { new [] { ";q=0.9" }, new string[] { } },
                { new [] { "/" }, new string[] { } },
                { new [] { "*/" }, new string[] { } },
                { new [] { "/*" }, new string[] { } },
                { new [] { "/;q=0.9" }, new string[] { } },
                { new [] { "*/;q=0.9" }, new string[] { } },
                { new [] { "/*;q=0.9" }, new string[] { } },
                { new [] { "/;q=0.9,text/html" }, new string[] { "text/html" } },
                { new [] { "*/;q=0.9,text/html" }, new string[] { "text/html" } },
                { new [] { "/*;q=0.9,text/html" }, new string[] { "text/html" } },
                { new [] { "img/png,/;q=0.9,text/html" }, new string[] { "img/png", "text/html" } },
                { new [] { "img/png,*/;q=0.9,text/html" }, new string[] { "img/png", "text/html" } },
                { new [] { "img/png,/*;q=0.9,text/html" }, new string[] { "img/png", "text/html" } },
                { new [] { "img/png, /;q=0.9" }, new string[] { "img/png", } },
                { new [] { "img/png, */;q=0.9" }, new string[] { "img/png", } },
                { new [] { "img/png;q=1.0, /*;q=0.9" }, new string[] { "img/png;q=1.0", } },
        };

    [Theory]
    [MemberData(nameof(ParseAcceptHeaderWithInvalidMediaTypesData))]
    public void ParseAcceptHeader_GracefullyRecoversFromInvalidMediaTypeValues_AndReturnsValidMediaTypes(
        string[] acceptHeader,
        string[] expected)
    {
        // Arrange
        var expectedMediaTypes = expected.Select(e => new MediaTypeSegmentWithQuality(new StringSegment(e), 1.0)).ToList();

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(acceptHeader);

        // Assert
        Assert.Equal(expectedMediaTypes, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_ParsesMultipleHeaderValues()
    {
        // Arrange
        var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"), 1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/xml;q=0.8"), 0.8)
            };

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(
            new List<string> { "application/json", "", "application/xml;q=0.8" });

        // Assert
        Assert.Equal(expected, parsed);
    }

    // The text "*/*Content-Type" parses as a valid media type value. However it's followed
    // by ':' instead of whitespace or a delimiter, which means that it's actually invalid.
    [Fact]
    public void ParseAcceptHeader_ValidMediaType_FollowedByNondelimiter()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[0];

        var input = "*/*Content-Type:application/json";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_ValidMediaType_FollowedBySemicolon()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[0];

        var input = "*/*Content-Type;application/json";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_ValidMediaType_FollowedByComma()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[]
        {
                new MediaTypeSegmentWithQuality(new StringSegment("*/*Content-Type"), 1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"), 1.0),
        };

        var input = "*/*Content-Type,application/json";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_ValidMediaType_FollowedByWhitespace()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[]
        {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"), 1.0),
        };

        var input = "*/*Content-Type application/json";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_InvalidTokenAtStart()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[0];

        var input = ":;:";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_DelimiterAtStart()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[0];

        var input = ",;:";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void ParseAcceptHeader_InvalidTokenAtEnd()
    {
        // Arrange
        var expected = new MediaTypeSegmentWithQuality[0];

        var input = "*/*:";

        // Act
        var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string>() { input });

        // Assert
        Assert.Equal(expected, parsed);
    }
}
