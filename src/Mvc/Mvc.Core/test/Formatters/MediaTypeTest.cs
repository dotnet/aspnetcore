// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters;

public class MediaTypeTest
{
    [Theory]
    [InlineData("application/json")]
    [InlineData("application /json")]
    [InlineData(" application / json ")]
    public void Constructor_CanParseParameterlessSuffixlessMediaTypes(string mediaType)
    {
        // Arrange & Act
        var result = new MediaType(mediaType, 0, mediaType.Length);

        // Assert
        Assert.Equal(new StringSegment("application"), result.Type);
        Assert.Equal(new StringSegment("json"), result.SubType);
    }

    public static IEnumerable<object[]> MediaTypesWithSuffixes
    {
        get
        {
            return new List<string[]>
                {
                    // See https://tools.ietf.org/html/rfc6838#section-4.2 for allowed names spec
                    new[] { "application/json", "json", null },
                    new[] { "application/json+", "json", "" },
                    new[] { "application/+json", "", "json" },
                    new[] { "application/entitytype+json", "entitytype", "json" },
                    new[] { "  application /  vnd.com-pany.some+entity!.v2+js.#$&^_n  ; q=\"0.3+1\"", "vnd.com-pany.some+entity!.v2", "js.#$&^_n" },
                };
        }
    }

    [Theory]
    [MemberData(nameof(MediaTypesWithSuffixes))]
    public void Constructor_CanParseSuffixedMediaTypes(
        string mediaType,
        string expectedSubTypeWithoutSuffix,
        string expectedSubtypeSuffix)
    {
        // Arrange & Act
        var result = new MediaType(mediaType);

        // Assert
        Assert.Equal(new StringSegment(expectedSubTypeWithoutSuffix), result.SubTypeWithoutSuffix);
        Assert.Equal(new StringSegment(expectedSubtypeSuffix), result.SubTypeSuffix);
    }

    public static TheoryData<string> MediaTypesWithParameters
    {
        get
        {
            return new TheoryData<string>
                {
                    "application/json+bson;format=pretty;charset=utf-8;q=0.8",
                    "application/json+bson;format=pretty;charset=\"utf-8\";q=0.8",
                    "application/json+bson;format=pretty;charset=utf-8; q=0.8 ",
                    "application/json+bson;format=pretty;charset=utf-8 ; q=0.8 ",
                    "application/json+bson;format=pretty; charset=utf-8 ; q=0.8 ",
                    "application/json+bson;format=pretty ; charset=utf-8 ; q=0.8 ",
                    "application/json+bson; format=pretty ; charset=utf-8 ; q=0.8 ",
                    "application/json+bson; format=pretty ; charset=utf-8 ; q=  0.8 ",
                    "application/json+bson; format=pretty ; charset=utf-8 ; q  =  0.8 ",
                    " application /  json+bson; format =  pretty ; charset = utf-8 ; q  =  0.8 ",
                    " application /  json+bson; format =  \"pretty\" ; charset = \"utf-8\" ; q  =  \"0.8\" ",
                };
        }
    }

    [Theory]
    [MemberData(nameof(MediaTypesWithParameters))]
    public void Constructor_CanParseMediaTypesWithParameters(string mediaType)
    {
        // Arrange & Act
        var result = new MediaType(mediaType, 0, mediaType.Length);

        // Assert
        Assert.Equal(new StringSegment("application"), result.Type);
        Assert.Equal(new StringSegment("json+bson"), result.SubType);
        Assert.Equal(new StringSegment("json"), result.SubTypeWithoutSuffix);
        Assert.Equal(new StringSegment("bson"), result.SubTypeSuffix);
        Assert.Equal(new StringSegment("pretty"), result.GetParameter("format"));
        Assert.Equal(new StringSegment("0.8"), result.GetParameter("q"));
        Assert.Equal(new StringSegment("utf-8"), result.GetParameter("charset"));
    }

    [Fact]
    public void Constructor_NullLength_IgnoresLength()
    {
        // Arrange & Act
        var result = new MediaType("mediaType", 1, length: null);

        // Assert
        Assert.Equal(new StringSegment("ediaType"), result.Type);
    }

    [Fact]
    public void Constructor_NullMediaType_Throws()
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentNullException>("mediaType", () => new MediaType(null, 0, 2));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Constructor_NegativeOffset_Throws(int offset)
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new MediaType("media", offset, 5));
    }

    [Fact]
    public void Constructor_NegativeLength_Throws()
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>("length", () => new MediaType("media", 0, -1));
    }

    [Fact]
    public void Constructor_OffsetOrLengthOutOfBounds_Throws()
    {
        // Arrange, Act and Assert
        Assert.Throws<ArgumentException>(() => new MediaType("lengthof9", 5, 5));
    }

    [Theory]
    [MemberData(nameof(MediaTypesWithParameters))]
    public void ReplaceEncoding_ReturnsExpectedMediaType(string mediaType)
    {
        // Arrange
        var encoding = Encoding.GetEncoding("iso-8859-1");
        var expectedMediaType = mediaType.Replace("utf-8", "iso-8859-1");

        // Act
        var result = MediaType.ReplaceEncoding(mediaType, encoding);

        // Assert
        Assert.Equal(expectedMediaType, result);
    }

    [Theory]
    [InlineData("application/json;charset=utf-8")]
    [InlineData("application/json;format=indent;q=0.8;charset=utf-8")]
    [InlineData("application/json;format=indent;charset=utf-8;q=0.8")]
    [InlineData("application/json;charset=utf-8;format=indent;q=0.8")]
    public void GetParameter_ReturnsParameter_IfParameterIsInMediaType(string mediaType)
    {
        // Arrange
        var expectedParameter = new StringSegment("utf-8");
        var parsedMediaType = new MediaType(mediaType, 0, mediaType.Length);

        // Act
        var result = parsedMediaType.GetParameter("charset");

        // Assert
        Assert.Equal(expectedParameter, result);
    }

    [Fact]
    public void GetParameter_ReturnsNull_IfParameterIsNotInMediaType()
    {
        var mediaType = "application/json;charset=utf-8;format=indent;q=0.8";

        var parsedMediaType = new MediaType(mediaType, 0, mediaType.Length);

        // Act
        var result = parsedMediaType.GetParameter("other");

        // Assert
        Assert.False(result.HasValue);
    }

    [Fact]
    public void GetParameter_IsCaseInsensitive()
    {
        // Arrange
        var mediaType = "application/json;charset=utf-8";
        var expectedParameter = new StringSegment("utf-8");

        var parsedMediaType = new MediaType(mediaType);

        // Act
        var result = parsedMediaType.GetParameter("CHARSET");

        // Assert
        Assert.Equal(expectedParameter, result);
    }

    [Theory]
    [InlineData("application/json", "application/json")]
    [InlineData("application/json", "application/json;charset=utf-8")]
    [InlineData("application/json;q=0.8", "application/json;q=0.9")]
    [InlineData("application/json;q=0.8;charset=utf-7", "application/json;charset=utf-8;q=0.9")]
    [InlineData("application/json", "application/json;format=indent;charset=utf-8")]
    [InlineData("application/json;format=indent;charset=utf-8", "application/json;format=indent;charset=utf-8")]
    [InlineData("application/json;charset=utf-8;format=indent", "application/json;format=indent;charset=utf-8")]
    [InlineData("application/*", "application/json")]
    [InlineData("application/*", "application/entitytype+json;v=2")]
    [InlineData("application/*;v=2", "application/entitytype+json;v=2")]
    [InlineData("application/json;*", "application/json;v=2")]
    [InlineData("application/json;v=2;*", "application/json;v=2;charset=utf-8")]
    [InlineData("*/*", "application/json")]
    [InlineData("application/entity+json", "application/entity+json")]
    [InlineData("application/*+json", "application/entity+json")]
    [InlineData("application/*", "application/entity+json")]
    [InlineData("application/json", "application/vnd.restful+json")]
    [InlineData("application/json", "application/problem+json")]
    public void IsSubsetOf_ReturnsTrueWhenExpected(string set, string subset)
    {
        // Arrange
        var setMediaType = new MediaType(set);
        var subSetMediaType = new MediaType(subset);

        // Act
        var result = subSetMediaType.IsSubsetOf(setMediaType);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/json;charset=utf-8", "application/json")]
    [InlineData("application/json;format=indent;charset=utf-8", "application/json")]
    [InlineData("application/json;format=indent;charset=utf-8", "application/json;charset=utf-8")]
    [InlineData("application/*", "text/json")]
    [InlineData("application/*;v=2", "application/json")]
    [InlineData("application/*;v=2", "application/json;v=1")]
    [InlineData("application/json;v=2;*", "application/json;v=1")]
    [InlineData("application/entity+json", "application/entity+txt")]
    [InlineData("application/entity+json", "application/entity.v2+json")]
    [InlineData("application/*+json", "application/entity+txt")]
    [InlineData("application/entity+*", "application/entity.v2+json")]
    [InlineData("application/*+*", "application/json")]
    [InlineData("application/entity+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/*+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/entity+json", "application/entity")]
    public void IsSubsetOf_ReturnsFalseWhenExpected(string set, string subset)
    {
        // Arrange
        var setMediaType = new MediaType(set);
        var subSetMediaType = new MediaType(subset);

        // Act
        var result = subSetMediaType.IsSubsetOf(setMediaType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MatchesAllTypes_ReturnsTrueWhenExpected()
    {
        // Arrange
        var mediaType = new MediaType("*/*");

        // Act
        var result = mediaType.MatchesAllTypes;

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("text/*")]
    [InlineData("text/plain")]
    public void MatchesAllTypes_ReturnsFalseWhenExpected(string value)
    {
        // Arrange
        var mediaType = new MediaType(value);

        // Act
        var result = mediaType.MatchesAllTypes;

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("*/*")]
    [InlineData("text/*")]
    public void MatchesAllSubtypes_ReturnsTrueWhenExpected(string value)
    {
        // Arrange
        var mediaType = new MediaType(value);

        // Act
        var result = mediaType.MatchesAllSubTypes;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MatchesAllSubtypes_ReturnsFalseWhenExpected()
    {
        // Arrange
        var mediaType = new MediaType("text/plain");

        // Act
        var result = mediaType.MatchesAllSubTypes;

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("*/*", true)]
    [InlineData("text/*", true)]
    [InlineData("text/*+suffix", true)]
    [InlineData("text/*+", true)]
    [InlineData("text/*+*", true)]
    [InlineData("text/json+suffix", false)]
    [InlineData("*/json+*", false)]
    public void MatchesAllSubTypesWithoutSuffix_ReturnsExpectedResult(string value, bool expectedReturnValue)
    {
        // Arrange
        var mediaType = new MediaType(value);

        // Act
        var result = mediaType.MatchesAllSubTypesWithoutSuffix;

        // Assert
        Assert.Equal(expectedReturnValue, result);
    }

    [Theory]
    [InlineData("*/*", true)]
    [InlineData("text/*", true)]
    [InlineData("text/entity+*", false)] // We don't support wildcards on suffixes
    [InlineData("text/*+json", true)]
    [InlineData("text/entity+json;*", true)]
    [InlineData("text/entity+json;v=3;*", true)]
    [InlineData("text/entity+json;v=3;q=0.8", false)]
    [InlineData("text/json", false)]
    [InlineData("text/json;param=*", false)] // * is the literal value of the param
    public void HasWildcard_ReturnsTrueWhenExpected(string value, bool expectedReturnValue)
    {
        // Arrange
        var mediaType = new MediaType(value);

        // Act
        var result = mediaType.HasWildcard;

        // Assert
        Assert.Equal(expectedReturnValue, result);
    }

    [Theory]
    [MemberData(nameof(MediaTypesWithParameters))]
    [InlineData("application/json;format=pretty;q=0.9;charset=utf-8;q=0.8")]
    [InlineData("application/json;format=pretty;q=0.9;charset=utf-8;q=0.8;version=3")]
    public void CreateMediaTypeSegmentWithQuality_FindsQValue(string value)
    {
        // Arrange & Act
        var mediaTypeSegment = MediaType.CreateMediaTypeSegmentWithQuality(value, start: 0);

        // Assert
        Assert.Equal(0.8d, mediaTypeSegment.Quality);
    }
}
