// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

public class MediaTypeHeaderValueTest
{
    [Fact]
    public void Ctor_MediaTypeNull_Throw()
    {
        Assert.Throws<ArgumentException>(() => new MediaTypeHeaderValue(null));
        // null and empty should be treated the same. So we also throw for empty strings.
        Assert.Throws<ArgumentException>(() => new MediaTypeHeaderValue(string.Empty));
    }

    [Fact]
    public void Ctor_MediaTypeInvalidFormat_ThrowFormatException()
    {
        // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
        AssertFormatException(" text/plain ");
        AssertFormatException("text / plain");
        AssertFormatException("text/ plain");
        AssertFormatException("text /plain");
        AssertFormatException("text/plain ");
        AssertFormatException(" text/plain");
        AssertFormatException("te xt/plain");
        AssertFormatException("te=xt/plain");
        AssertFormatException("teäxt/plain");
        AssertFormatException("text/pläin");
        AssertFormatException("text");
        AssertFormatException("\"text/plain\"");
        AssertFormatException("text/plain; charset=utf-8; ");
        AssertFormatException("text/plain;");
        AssertFormatException("text/plain;charset=utf-8"); // ctor takes only media-type name, no parameters
    }

    public static TheoryData<string, string, string?> MediaTypesWithSuffixes =>
             new TheoryData<string, string, string?>
             {
                     // See https://tools.ietf.org/html/rfc6838#section-4.2 for allowed names spec
                     { "application/json", "json", null },
                     { "application/json+", "json", "" },
                     { "application/+json", "", "json" },
                     { "application/entitytype+json", "entitytype", "json" },
                     { "applica+tion/entitytype+json", "entitytype", "json" },
             };

    [Theory]
    [MemberData(nameof(MediaTypesWithSuffixes))]
    public void Ctor_CanParseSuffixedMediaTypes(string mediaType, string expectedSubTypeWithoutSuffix, string? expectedSubTypeSuffix)
    {
        var result = new MediaTypeHeaderValue(mediaType);

        Assert.Equal(new StringSegment(expectedSubTypeWithoutSuffix), result.SubTypeWithoutSuffix); // TODO consider overloading to have SubTypeWithoutSuffix?
        Assert.Equal(new StringSegment(expectedSubTypeSuffix), result.Suffix);
    }

    public static TheoryData<string, string, string> MediaTypesWithSuffixesAndSpaces =>
             new TheoryData<string, string, string>
             {
                     // See https://tools.ietf.org/html/rfc6838#section-4.2 for allowed names spec
                     { "    application   /  json+xml", "json", "xml" },
                     { "  application /  vnd.com-pany.some+entity!.v2+js.#$&^_n  ; q=\"0.3+1\"", "vnd.com-pany.some+entity!.v2", "js.#$&^_n"},
                     { "   application/    +json", "", "json" },
                     { "  application/   entitytype+json    ", "entitytype", "json" },
                     { "  applica+tion/   entitytype+json    ", "entitytype", "json" }
             };

    [Theory]
    [MemberData(nameof(MediaTypesWithSuffixesAndSpaces))]
    public void Parse_CanParseSuffixedMediaTypes(string mediaType, string expectedSubTypeWithoutSuffix, string expectedSubTypeSuffix)
    {
        var result = MediaTypeHeaderValue.Parse(mediaType);

        Assert.Equal(new StringSegment(expectedSubTypeWithoutSuffix), result.SubTypeWithoutSuffix); // TODO consider overloading to have SubTypeWithoutSuffix?
        Assert.Equal(new StringSegment(expectedSubTypeSuffix), result.Suffix);
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
        var mediaType = new MediaTypeHeaderValue(value);

        // Act
        var result = mediaType.MatchesAllSubTypesWithoutSuffix;

        // Assert
        Assert.Equal(expectedReturnValue, result);
    }

    [Fact]
    public void Ctor_MediaTypeValidFormat_SuccessfullyCreated()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        Assert.Equal("text/plain", mediaType.MediaType.AsSpan());
        Assert.Empty(mediaType.Parameters);
        Assert.Null(mediaType.Charset.Value);
    }

    [Fact]
    public void Ctor_AddNameAndQuality_QualityParameterAdded()
    {
        var mediaType = new MediaTypeHeaderValue("application/xml", 0.08);
        Assert.Equal(0.08, mediaType.Quality);
        Assert.Equal("application/xml", mediaType.MediaType.AsSpan());
        Assert.Single(mediaType.Parameters);
    }

    [Fact]
    public void Parameters_AddNull_Throw()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        Assert.Throws<ArgumentNullException>(() => mediaType.Parameters.Add(null!));
    }

    [Fact]
    public void Copy_SimpleMediaType_Copied()
    {
        var mediaType0 = new MediaTypeHeaderValue("text/plain");
        var mediaType1 = mediaType0.Copy();
        Assert.NotSame(mediaType0, mediaType1);
        Assert.Same(mediaType0.MediaType.Value, mediaType1.MediaType.Value);
        Assert.NotSame(mediaType0.Parameters, mediaType1.Parameters);
        Assert.Equal(mediaType0.Parameters.Count, mediaType1.Parameters.Count);
    }

    [Fact]
    public void CopyAsReadOnly_SimpleMediaType_CopiedAndReadOnly()
    {
        var mediaType0 = new MediaTypeHeaderValue("text/plain");
        var mediaType1 = mediaType0.CopyAsReadOnly();
        Assert.NotSame(mediaType0, mediaType1);
        Assert.Same(mediaType0.MediaType.Value, mediaType1.MediaType.Value);
        Assert.NotSame(mediaType0.Parameters, mediaType1.Parameters);
        Assert.Equal(mediaType0.Parameters.Count, mediaType1.Parameters.Count);

        Assert.False(mediaType0.IsReadOnly);
        Assert.True(mediaType1.IsReadOnly);
        Assert.Throws<InvalidOperationException>(() => { mediaType1.MediaType = "some/value"; });
    }

    [Fact]
    public void Copy_WithParameters_Copied()
    {
        var mediaType0 = new MediaTypeHeaderValue("text/plain");
        mediaType0.Parameters.Add(new NameValueHeaderValue("name", "value"));
        var mediaType1 = mediaType0.Copy();
        Assert.NotSame(mediaType0, mediaType1);
        Assert.Same(mediaType0.MediaType.Value, mediaType1.MediaType.Value);
        Assert.NotSame(mediaType0.Parameters, mediaType1.Parameters);
        Assert.Equal(mediaType0.Parameters.Count, mediaType1.Parameters.Count);
        var pair0 = mediaType0.Parameters.First();
        var pair1 = mediaType1.Parameters.First();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Same(pair0.Value.Value, pair1.Value.Value);
    }

    [Fact]
    public void CopyAsReadOnly_WithParameters_CopiedAndReadOnly()
    {
        var mediaType0 = new MediaTypeHeaderValue("text/plain");
        mediaType0.Parameters.Add(new NameValueHeaderValue("name", "value"));
        var mediaType1 = mediaType0.CopyAsReadOnly();
        Assert.NotSame(mediaType0, mediaType1);
        Assert.False(mediaType0.IsReadOnly);
        Assert.True(mediaType1.IsReadOnly);
        Assert.Same(mediaType0.MediaType.Value, mediaType1.MediaType.Value);

        Assert.NotSame(mediaType0.Parameters, mediaType1.Parameters);
        Assert.False(mediaType0.Parameters.IsReadOnly);
        Assert.True(mediaType1.Parameters.IsReadOnly);
        Assert.Equal(mediaType0.Parameters.Count, mediaType1.Parameters.Count);
        Assert.Throws<NotSupportedException>(() => mediaType1.Parameters.Add(new NameValueHeaderValue("name")));
        Assert.Throws<NotSupportedException>(() => mediaType1.Parameters.Remove(new NameValueHeaderValue("name")));
        Assert.Throws<NotSupportedException>(() => mediaType1.Parameters.Clear());

        var pair0 = mediaType0.Parameters.First();
        var pair1 = mediaType1.Parameters.First();
        Assert.NotSame(pair0, pair1);
        Assert.False(pair0.IsReadOnly);
        Assert.True(pair1.IsReadOnly);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Same(pair0.Value.Value, pair1.Value.Value);
    }

    [Fact]
    public void CopyFromReadOnly_WithParameters_CopiedAsNonReadOnly()
    {
        var mediaType0 = new MediaTypeHeaderValue("text/plain");
        mediaType0.Parameters.Add(new NameValueHeaderValue("name", "value"));
        var mediaType1 = mediaType0.CopyAsReadOnly();
        var mediaType2 = mediaType1.Copy();

        Assert.NotSame(mediaType2, mediaType1);
        Assert.Same(mediaType2.MediaType.Value, mediaType1.MediaType.Value);
        Assert.True(mediaType1.IsReadOnly);
        Assert.False(mediaType2.IsReadOnly);
        Assert.NotSame(mediaType2.Parameters, mediaType1.Parameters);
        Assert.Equal(mediaType2.Parameters.Count, mediaType1.Parameters.Count);
        var pair2 = mediaType2.Parameters.First();
        var pair1 = mediaType1.Parameters.First();
        Assert.NotSame(pair2, pair1);
        Assert.True(pair1.IsReadOnly);
        Assert.False(pair2.IsReadOnly);
        Assert.Same(pair2.Name.Value, pair1.Name.Value);
        Assert.Same(pair2.Value.Value, pair1.Value.Value);
    }

    [Fact]
    public void MediaType_SetAndGetMediaType_MatchExpectations()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        Assert.Equal("text/plain", mediaType.MediaType.AsSpan());

        mediaType.MediaType = "application/xml";
        Assert.Equal("application/xml", mediaType.MediaType.AsSpan());
    }

    [Fact]
    public void Charset_SetCharsetAndValidateObject_ParametersEntryForCharsetAdded()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        mediaType.Charset = "mycharset";
        Assert.Equal("mycharset", mediaType.Charset.AsSpan());
        Assert.Single(mediaType.Parameters);
        Assert.Equal("charset", mediaType.Parameters.First().Name.AsSpan());

        mediaType.Charset = null;
        Assert.Null(mediaType.Charset.Value);
        Assert.Empty(mediaType.Parameters);
        mediaType.Charset = null; // It's OK to set it again to null; no exception.
    }

    [Fact]
    public void Charset_AddCharsetParameterThenUseProperty_ParametersEntryIsOverwritten()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");

        // Note that uppercase letters are used. Comparison should happen case-insensitive.
        var charset = new NameValueHeaderValue("CHARSET", "old_charset");
        mediaType.Parameters.Add(charset);
        Assert.Single(mediaType.Parameters);
        Assert.Equal("CHARSET", mediaType.Parameters.First().Name.AsSpan());

        mediaType.Charset = "new_charset";
        Assert.Equal("new_charset", mediaType.Charset.AsSpan());
        Assert.Single(mediaType.Parameters);
        Assert.Equal("CHARSET", mediaType.Parameters.First().Name.AsSpan());

        mediaType.Parameters.Remove(charset);
        Assert.Null(mediaType.Charset.Value);
    }

    [Fact]
    public void Quality_SetCharsetAndValidateObject_ParametersEntryForCharsetAdded()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        mediaType.Quality = 0.563156454;
        Assert.Equal(0.563, mediaType.Quality);
        Assert.Single(mediaType.Parameters);
        Assert.Equal("q", mediaType.Parameters.First().Name.AsSpan());
        Assert.Equal("0.563", mediaType.Parameters.First().Value.AsSpan());

        mediaType.Quality = null;
        Assert.Null(mediaType.Quality);
        Assert.Empty(mediaType.Parameters);
        mediaType.Quality = null; // It's OK to set it again to null; no exception.
    }

    [Fact]
    public void Quality_AddQualityParameterThenUseProperty_ParametersEntryIsOverwritten()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");

        var quality = new NameValueHeaderValue("q", "0.132");
        mediaType.Parameters.Add(quality);
        Assert.Single(mediaType.Parameters);
        Assert.Equal("q", mediaType.Parameters.First().Name.AsSpan());
        Assert.Equal(0.132, mediaType.Quality);

        mediaType.Quality = 0.9;
        Assert.Equal(0.9, mediaType.Quality);
        Assert.Single(mediaType.Parameters);
        Assert.Equal("q", mediaType.Parameters.First().Name.AsSpan());

        mediaType.Parameters.Remove(quality);
        Assert.Null(mediaType.Quality);
    }

    [Fact]
    public void Quality_AddQualityParameterUpperCase_CaseInsensitiveComparison()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");

        var quality = new NameValueHeaderValue("Q", "0.132");
        mediaType.Parameters.Add(quality);
        Assert.Single(mediaType.Parameters);
        Assert.Equal("Q", mediaType.Parameters.First().Name.AsSpan());
        Assert.Equal(0.132, mediaType.Quality);
    }

    [Fact]
    public void Quality_LessThanZero_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MediaTypeHeaderValue("application/xml", -0.01));
    }

    [Fact]
    public void Quality_GreaterThanOne_Throw()
    {
        var mediaType = new MediaTypeHeaderValue("application/xml");
        Assert.Throws<ArgumentOutOfRangeException>(() => mediaType.Quality = 1.01);
    }

    [Fact]
    public void ToString_UseDifferentMediaTypes_AllSerializedCorrectly()
    {
        var mediaType = new MediaTypeHeaderValue("text/plain");
        Assert.Equal("text/plain", mediaType.ToString());

        mediaType.Charset = "utf-8";
        Assert.Equal("text/plain; charset=utf-8", mediaType.ToString());

        mediaType.Parameters.Add(new NameValueHeaderValue("custom", "\"custom value\""));
        Assert.Equal("text/plain; charset=utf-8; custom=\"custom value\"", mediaType.ToString());

        mediaType.Charset = null;
        Assert.Equal("text/plain; custom=\"custom value\"", mediaType.ToString());
    }

    [Fact]
    public void GetHashCode_UseMediaTypeWithAndWithoutParameters_SameOrDifferentHashCodes()
    {
        var mediaType1 = new MediaTypeHeaderValue("text/plain");
        var mediaType2 = new MediaTypeHeaderValue("text/plain");
        mediaType2.Charset = "utf-8";
        var mediaType3 = new MediaTypeHeaderValue("text/plain");
        mediaType3.Parameters.Add(new NameValueHeaderValue("name", "value"));
        var mediaType4 = new MediaTypeHeaderValue("TEXT/plain");
        var mediaType5 = new MediaTypeHeaderValue("TEXT/plain");
        mediaType5.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));

        Assert.NotEqual(mediaType1.GetHashCode(), mediaType2.GetHashCode());
        Assert.NotEqual(mediaType1.GetHashCode(), mediaType3.GetHashCode());
        Assert.NotEqual(mediaType2.GetHashCode(), mediaType3.GetHashCode());
        Assert.Equal(mediaType1.GetHashCode(), mediaType4.GetHashCode());
        Assert.Equal(mediaType2.GetHashCode(), mediaType5.GetHashCode());
    }

    [Fact]
    public void Equals_UseMediaTypeWithAndWithoutParameters_EqualOrNotEqualNoExceptions()
    {
        var mediaType1 = new MediaTypeHeaderValue("text/plain");
        var mediaType2 = new MediaTypeHeaderValue("text/plain");
        mediaType2.Charset = "utf-8";
        var mediaType3 = new MediaTypeHeaderValue("text/plain");
        mediaType3.Parameters.Add(new NameValueHeaderValue("name", "value"));
        var mediaType4 = new MediaTypeHeaderValue("TEXT/plain");
        var mediaType5 = new MediaTypeHeaderValue("TEXT/plain");
        mediaType5.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));
        var mediaType6 = new MediaTypeHeaderValue("TEXT/plain");
        mediaType6.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));
        mediaType6.Parameters.Add(new NameValueHeaderValue("custom", "value"));
        var mediaType7 = new MediaTypeHeaderValue("text/other");

        Assert.False(mediaType1.Equals(mediaType2), "No params vs. charset.");
        Assert.False(mediaType2.Equals(mediaType1), "charset vs. no params.");
        Assert.False(mediaType1.Equals(null), "No params vs. <null>.");
        Assert.False(mediaType1!.Equals(mediaType3), "No params vs. custom param.");
        Assert.False(mediaType2.Equals(mediaType3), "charset vs. custom param.");
        Assert.True(mediaType1.Equals(mediaType4), "Different casing.");
        Assert.True(mediaType2.Equals(mediaType5), "Different casing in charset.");
        Assert.False(mediaType5.Equals(mediaType6), "charset vs. custom param.");
        Assert.False(mediaType1.Equals(mediaType7), "text/plain vs. text/other.");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse("\r\n text/plain  ", new MediaTypeHeaderValue("text/plain"));
        CheckValidParse("text/plain", new MediaTypeHeaderValue("text/plain"));

        CheckValidParse("\r\n text   /  plain ;  charset =   utf-8 ", new MediaTypeHeaderValue("text/plain") { Charset = "utf-8" });
        CheckValidParse("  text/plain;charset=utf-8", new MediaTypeHeaderValue("text/plain") { Charset = "utf-8" });

        CheckValidParse("text/plain; charset=iso-8859-1", new MediaTypeHeaderValue("text/plain") { Charset = "iso-8859-1" });

        var expected = new MediaTypeHeaderValue("text/plain") { Charset = "utf-8" };
        expected.Parameters.Add(new NameValueHeaderValue("custom", "value"));
        CheckValidParse(" text/plain; custom=value;charset=utf-8", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        expected.Parameters.Add(new NameValueHeaderValue("custom"));
        CheckValidParse(" text/plain; custom", expected);

        expected = new MediaTypeHeaderValue("text/plain") { Charset = "utf-8" };
        expected.Parameters.Add(new NameValueHeaderValue("custom", "\"x\""));
        CheckValidParse("text / plain ; custom =\r\n \"x\" ; charset = utf-8 ", expected);

        expected = new MediaTypeHeaderValue("text/plain") { Charset = "utf-8" };
        expected.Parameters.Add(new NameValueHeaderValue("custom", "\"x\""));
        CheckValidParse("text/plain;custom=\"x\";charset=utf-8", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        CheckValidParse("text/plain;", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        expected.Parameters.Add(new NameValueHeaderValue("name", ""));
        CheckValidParse("text/plain;name=", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        expected.Parameters.Add(new NameValueHeaderValue("name", "value"));
        CheckValidParse("text/plain;name=value;", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        expected.Charset = "iso-8859-1";
        expected.Quality = 1.0;
        CheckValidParse("text/plain; charset=iso-8859-1; q=1.0", expected);

        expected = new MediaTypeHeaderValue("*/xml");
        expected.Charset = "utf-8";
        expected.Quality = 0.5;
        CheckValidParse("\r\n */xml; charset=utf-8; q=0.5", expected);

        expected = new MediaTypeHeaderValue("*/*");
        CheckValidParse("*/*", expected);

        expected = new MediaTypeHeaderValue("text/*");
        expected.Charset = "utf-8";
        expected.Parameters.Add(new NameValueHeaderValue("foo", "bar"));
        CheckValidParse("text/*; charset=utf-8; foo=bar", expected);

        expected = new MediaTypeHeaderValue("text/plain");
        expected.Charset = "utf-8";
        expected.Quality = 0;
        expected.Parameters.Add(new NameValueHeaderValue("foo", "bar"));
        CheckValidParse("text/plain; charset=utf-8; foo=bar; q=0.0", expected);
    }

    [Fact]
    public void Parse_SetOfInvalidValueStrings_Throws()
    {
        CheckInvalidParse("");
        CheckInvalidParse("  ");
        CheckInvalidParse(null);
        CheckInvalidParse("text/plain会");
        CheckInvalidParse("text/plain ,");
        CheckInvalidParse("text/plain,");
        CheckInvalidParse("text/plain; charset=utf-8 ,");
        CheckInvalidParse("text/plain; charset=utf-8,");
        CheckInvalidParse("textplain");
        CheckInvalidParse("text/");
        CheckInvalidParse(",, , ,,text/plain; charset=iso-8859-1; q=1.0,\r\n */xml; charset=utf-8; q=0.5,,,");
        CheckInvalidParse("text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5");
        CheckInvalidParse(" , */xml; charset=utf-8; q=0.5 ");
        CheckInvalidParse("text/plain; charset=iso-8859-1; q=1.0 , ");
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        var expected = new MediaTypeHeaderValue("text/plain");
        CheckValidTryParse("\r\n text/plain  ", expected);
        CheckValidTryParse("text/plain", expected);

        // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
        // The purpose of this test is to verify that these other parsers are combined correctly to build a
        // media-type parser.
        expected.Charset = "utf-8";
        CheckValidTryParse("\r\n text   /  plain ;  charset =   utf-8 ", expected);
        CheckValidTryParse("  text/plain;charset=utf-8", expected);

        var value1 = new MediaTypeHeaderValue("text/plain");
        value1.Charset = "iso-8859-1";
        value1.Quality = 1.0;

        CheckValidTryParse("text/plain; charset=iso-8859-1; q=1.0", value1);

        var value2 = new MediaTypeHeaderValue("*/xml");
        value2.Charset = "utf-8";
        value2.Quality = 0.5;

        CheckValidTryParse("\r\n */xml; charset=utf-8; q=0.5", value2);
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse("");
        CheckInvalidTryParse("  ");
        CheckInvalidTryParse(null);
        CheckInvalidTryParse("text/plain会");
        CheckInvalidTryParse("text/plain ,");
        CheckInvalidTryParse("text/plain,");
        CheckInvalidTryParse("text/plain; charset=utf-8 ,");
        CheckInvalidTryParse("text/plain; charset=utf-8,");
        CheckInvalidTryParse("textplain");
        CheckInvalidTryParse("text/");
        CheckInvalidTryParse(",, , ,,text/plain; charset=iso-8859-1; q=1.0,\r\n */xml; charset=utf-8; q=0.5,,,");
        CheckInvalidTryParse("text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5");
        CheckInvalidTryParse(" , */xml; charset=utf-8; q=0.5 ");
        CheckInvalidTryParse("text/plain; charset=iso-8859-1; q=1.0 , ");
    }

    [Fact]
    public void ParseList_NullOrEmptyArray_ReturnsEmptyList()
    {
        var results = MediaTypeHeaderValue.ParseList(null);
        Assert.NotNull(results);
        Assert.Empty(results);

        results = MediaTypeHeaderValue.ParseList(new string[0]);
        Assert.NotNull(results);
        Assert.Empty(results);

        results = MediaTypeHeaderValue.ParseList(new string[] { "" });
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public void TryParseList_NullOrEmptyArray_ReturnsFalse()
    {
        Assert.False(MediaTypeHeaderValue.TryParseList(null, out var results));
        Assert.False(MediaTypeHeaderValue.TryParseList(new string[0], out results));
        Assert.False(MediaTypeHeaderValue.TryParseList(new string[] { "" }, out results));
    }

    [Fact]
    public void ParseList_SetOfValidValueStrings_ReturnsValues()
    {
        var inputs = new[] { "text/html,application/xhtml+xml,", "application/xml;q=0.9,image/webp,*/*;q=0.8" };
        var results = MediaTypeHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_SetOfValidValueStrings_ReturnsValues()
    {
        var inputs = new[] { "text/html,application/xhtml+xml,", "application/xml;q=0.9,image/webp,*/*;q=0.8" };
        var results = MediaTypeHeaderValue.ParseStrictList(inputs);

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseList_SetOfValidValueStrings_ReturnsTrue()
    {
        var inputs = new[] { "text/html,application/xhtml+xml,", "application/xml;q=0.9,image/webp,*/*;q=0.8" };
        Assert.True(MediaTypeHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_SetOfValidValueStrings_ReturnsTrue()
    {
        var inputs = new[] { "text/html,application/xhtml+xml,", "application/xml;q=0.9,image/webp,*/*;q=0.8" };
        Assert.True(MediaTypeHeaderValue.TryParseStrictList(inputs, out var results));

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseList_WithSomeInvalidValues_IgnoresInvalidValues()
    {
        var inputs = new[]
        {
                "text/html,application/xhtml+xml, ignore-this, ignore/this",
                "application/xml;q=0.9,image/webp,*/*;q=0.8"
            };
        var results = MediaTypeHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("ignore/this"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_WithSomeInvalidValues_Throws()
    {
        var inputs = new[]
        {
                "text/html,application/xhtml+xml, ignore-this, ignore/this",
                "application/xml;q=0.9,image/webp,*/*;q=0.8"
            };
        Assert.Throws<FormatException>(() => MediaTypeHeaderValue.ParseStrictList(inputs));
    }

    [Fact]
    public void TryParseList_WithSomeInvalidValues_IgnoresInvalidValues()
    {
        var inputs = new[]
        {
                "text/html,application/xhtml+xml, ignore-this, ignore/this",
                "application/xml;q=0.9,image/webp,*/*;q=0.8",
                "application/xml;q=0 4"
            };
        Assert.True(MediaTypeHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new MediaTypeHeaderValue("text/html"),
                new MediaTypeHeaderValue("application/xhtml+xml"),
                new MediaTypeHeaderValue("ignore/this"),
                new MediaTypeHeaderValue("application/xml", 0.9),
                new MediaTypeHeaderValue("image/webp"),
                new MediaTypeHeaderValue("*/*", 0.8),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_WithSomeInvalidValues_ReturnsFalse()
    {
        var inputs = new[]
        {
                "text/html,application/xhtml+xml, ignore-this, ignore/this",
                "application/xml;q=0.9,image/webp,*/*;q=0.8",
                "application/xml;q=0 4"
            };
        Assert.False(MediaTypeHeaderValue.TryParseStrictList(inputs, out var results));
    }

    [Theory]
    [InlineData("*/*;", "*/*")]
    [InlineData("text/*", "text/*")]
    [InlineData("text/*", "text/plain")]
    [InlineData("*/*;", "text/plain")]
    [InlineData("text/plain", "text/plain")]
    [InlineData("text/plain;", "text/plain")]
    [InlineData("text/plain;", "TEXT/PLAIN")]
    public void MatchesMediaType_PositiveCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Act
        var matches = parsedMediaType1.MatchesMediaType(mediaType2);
        var isSubsetOf = parsedMediaType2.IsSubsetOf(parsedMediaType1);

        // Assert
        Assert.True(matches);
        //Make sure that MatchesMediaType produces consistent result with IsSubsetOf
        Assert.Equal(matches, isSubsetOf);
    }

    [Theory]
    [InlineData("application/html", "text/*")]
    [InlineData("application/json", "application/html")]
    [InlineData("text/plain;", "*/*")]
    public void MatchesMediaType_NegativeCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Act
        var matches = parsedMediaType1.MatchesMediaType(mediaType2);
        var isSubsetOf = parsedMediaType2.IsSubsetOf(parsedMediaType1);

        // Assert
        Assert.False(matches);
        //Make sure that MatchesMediaType produces consistent result with IsSubsetOf
        Assert.Equal(matches, isSubsetOf);
    }

    [Theory]
    [InlineData("application/entity+json", "application/entity+json")]
    [InlineData("application/json", "application/entity+json")]
    [InlineData("application/*+json", "application/entity+json")]
    [InlineData("application/*+json", "application/*+json")]
    [InlineData("application/json", "application/problem+json")]
    [InlineData("application/json", "application/vnd.restful+json")]
    [InlineData("application/*", "application/*+JSON")]
    [InlineData("application/*", "application/entity+JSON")]
    [InlineData("*/*", "application/entity+json")]
    public void MatchesMediaTypeWithSuffixes_PositiveCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Act
        var result = parsedMediaType1.MatchesMediaType(mediaType2);
        var isSubsetOf = parsedMediaType2.IsSubsetOf(parsedMediaType1);

        // Assert
        Assert.True(result);
        //Make sure that MatchesMediaType produces consistent result with IsSubsetOf
        Assert.Equal(result, isSubsetOf);
    }

    [Theory]
    [InlineData("application/entity+json", "application/entity+txt")]
    [InlineData("application/entity+json", "application/json")]
    [InlineData("application/entity+json", "application/entity.v2+json")]
    [InlineData("application/*+json", "application/entity+txt")]
    [InlineData("application/*+*", "application/json")]
    [InlineData("application/entity", "application/entity+")]
    [InlineData("application/entity+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/*+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/entity+json", "application/entity")]
    public void MatchesMediaTypeWithSuffixes_NegativeCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Arrange
        var result = parsedMediaType1.MatchesMediaType(mediaType2);
        var isSubsetOf = parsedMediaType2.IsSubsetOf(parsedMediaType1);

        // Assert
        Assert.False(result);
        //Make sure that MatchesMediaType produces consistent result with IsSubsetOf
        Assert.Equal(result, isSubsetOf);
    }

    [Fact]
    public void MatchesMediaType_IgnoresParameters()
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse("application/json;param=1");

        // Arrange
        var result = parsedMediaType1.MatchesMediaType("application/json;param2=1");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("*/*;", "*/*")]
    [InlineData("text/*", "text/*")]
    [InlineData("text/*;", "*/*")]
    [InlineData("text/plain;", "text/plain")]
    [InlineData("text/plain", "text/*")]
    [InlineData("text/plain;", "*/*")]
    [InlineData("*/*;missingparam=4", "*/*")]
    [InlineData("text/*;missingparam=4;", "*/*;")]
    [InlineData("text/plain;missingparam=4", "*/*;")]
    [InlineData("text/plain;missingparam=4", "text/*")]
    [InlineData("text/plain;charset=utf-8", "text/plain;charset=utf-8")]
    [InlineData("text/plain;version=v1", "Text/plain;Version=v1")]
    [InlineData("text/plain;version=v1", "tExT/plain;version=V1")]
    [InlineData("text/plain;version=v1", "TEXT/PLAIN;VERSION=V1")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/plain;charset=utf-8;foo=bar;q=0.0")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/plain;foo=bar;q=0.0;charset=utf-8")] // different order of parameters
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/*;charset=utf-8;foo=bar;q=0.0")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "*/*;charset=utf-8;foo=bar;q=0.0")]
    [InlineData("application/json;v=2", "application/json;*")]
    [InlineData("application/json;v=2;charset=utf-8", "application/json;v=2;*")]
    public void IsSubsetOf_PositiveCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Act
        var isSubset = parsedMediaType1.IsSubsetOf(parsedMediaType2);

        // Assert
        Assert.True(isSubset);
    }

    [Theory]
    [InlineData("application/html", "text/*")]
    [InlineData("application/json", "application/html")]
    [InlineData("text/plain;version=v1", "text/plain;version=")]
    [InlineData("*/*;", "text/plain;charset=utf-8;foo=bar;q=0.0")]
    [InlineData("text/*;", "text/plain;charset=utf-8;foo=bar;q=0.0")]
    [InlineData("text/*;charset=utf-8;foo=bar;q=0.0", "text/plain;missingparam=4;")]
    [InlineData("*/*;charset=utf-8;foo=bar;q=0.0", "text/plain;missingparam=4;")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/plain;missingparam=4;")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/*;missingparam=4;")]
    [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "*/*;missingparam=4;")]
    public void IsSubsetOf_NegativeCases(string mediaType1, string mediaType2)
    {
        // Arrange
        var parsedMediaType1 = MediaTypeHeaderValue.Parse(mediaType1);
        var parsedMediaType2 = MediaTypeHeaderValue.Parse(mediaType2);

        // Act
        var isSubset = parsedMediaType1.IsSubsetOf(parsedMediaType2);

        // Assert
        Assert.False(isSubset);
    }

    [Theory]
    [InlineData("application/entity+json", "application/entity+json")]
    [InlineData("application/*+json", "application/entity+json")]
    [InlineData("application/*+json", "application/*+json")]
    [InlineData("application/json", "application/problem+json")]
    [InlineData("application/json", "application/vnd.restful+json")]
    [InlineData("application/*", "application/*+JSON")]
    [InlineData("application/vnd.github+json", "application/vnd.github+json")]
    [InlineData("application/*", "application/entity+JSON")]
    [InlineData("*/*", "application/entity+json")]
    public void IsSubsetOfWithSuffixes_PositiveCases(string set, string subset)
    {
        // Arrange
        var setMediaType = MediaTypeHeaderValue.Parse(set);
        var subSetMediaType = MediaTypeHeaderValue.Parse(subset);

        // Act
        var result = subSetMediaType.IsSubsetOf(setMediaType);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/entity+json", "application/entity+txt")]
    [InlineData("application/entity+json", "application/entity.v2+json")]
    [InlineData("application/*+json", "application/entity+txt")]
    [InlineData("application/*+*", "application/json")]
    [InlineData("application/entity+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/*+*", "application/entity+json")] // We don't allow suffixes to be wildcards
    [InlineData("application/entity+json", "application/entity")]
    public void IsSubSetOfWithSuffixes_NegativeCases(string set, string subset)
    {
        // Arrange
        var setMediaType = MediaTypeHeaderValue.Parse(set);
        var subSetMediaType = MediaTypeHeaderValue.Parse(subset);

        // Act
        var result = subSetMediaType.IsSubsetOf(setMediaType);

        // Assert
        Assert.False(result);
    }

    public static TheoryData<string, List<StringSegment>> MediaTypesWithFacets =>
             new TheoryData<string, List<StringSegment>>
             {
                     { "application/vdn.github",
                         new List<StringSegment>(){ "vdn", "github" } },
                     { "application/vdn.github+json",
                         new List<StringSegment>(){ "vdn", "github" } },
                     { "application/vdn.github.v3+json",
                         new List<StringSegment>(){ "vdn", "github", "v3" } },
                     { "application/vdn.github.+json",
                         new List<StringSegment>(){ "vdn", "github", "" } },
             };

    [Theory]
    [MemberData(nameof(MediaTypesWithFacets))]
    public void Facets_TestPositiveCases(string input, List<StringSegment> expected)
    {
        // Arrange
        var mediaType = MediaTypeHeaderValue.Parse(input);

        // Act
        var result = mediaType.Facets;

        // Assert
        Assert.Equal(expected, result);
    }

    private void CheckValidParse(string? input, MediaTypeHeaderValue expectedResult)
    {
        var result = MediaTypeHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidParse(string? input)
    {
        Assert.Throws<FormatException>(() => MediaTypeHeaderValue.Parse(input));
    }

    private void CheckValidTryParse(string? input, MediaTypeHeaderValue expectedResult)
    {
        Assert.True(MediaTypeHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(MediaTypeHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    private static void AssertFormatException(string mediaType)
    {
        Assert.Throws<FormatException>(() => new MediaTypeHeaderValue(mediaType));
    }
}
