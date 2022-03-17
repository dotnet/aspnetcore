// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class StringWithQualityHeaderValueTest
{
    [Fact]
    public void Ctor_StringOnlyOverload_MatchExpectation()
    {
        var value = new StringWithQualityHeaderValue("token");
        Assert.Equal("token", value.Value);
        Assert.Null(value.Quality);

        Assert.Throws<ArgumentException>(() => new StringWithQualityHeaderValue(null));
        Assert.Throws<ArgumentException>(() => new StringWithQualityHeaderValue(""));
        Assert.Throws<FormatException>(() => new StringWithQualityHeaderValue("in valid"));
    }

    [Fact]
    public void Ctor_StringWithQualityOverload_MatchExpectation()
    {
        var value = new StringWithQualityHeaderValue("token", 0.5);
        Assert.Equal("token", value.Value);
        Assert.Equal(0.5, value.Quality);

        Assert.Throws<ArgumentException>(() => new StringWithQualityHeaderValue(null, 0.1));
        Assert.Throws<ArgumentException>(() => new StringWithQualityHeaderValue("", 0.1));
        Assert.Throws<FormatException>(() => new StringWithQualityHeaderValue("in valid", 0.1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new StringWithQualityHeaderValue("t", 1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StringWithQualityHeaderValue("t", -0.1));
    }

    [Fact]
    public void ToString_UseDifferentValues_AllSerializedCorrectly()
    {
        var value = new StringWithQualityHeaderValue("token");
        Assert.Equal("token", value.ToString());

        value = new StringWithQualityHeaderValue("token", 0.1);
        Assert.Equal("token; q=0.1", value.ToString());

        value = new StringWithQualityHeaderValue("token", 0);
        Assert.Equal("token; q=0.0", value.ToString());

        value = new StringWithQualityHeaderValue("token", 1);
        Assert.Equal("token; q=1.0", value.ToString());

        // Note that the quality value gets rounded
        value = new StringWithQualityHeaderValue("token", 0.56789);
        Assert.Equal("token; q=0.568", value.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentValues_SameOrDifferentHashCodes()
    {
        var value1 = new StringWithQualityHeaderValue("t", 0.123);
        var value2 = new StringWithQualityHeaderValue("t", 0.123);
        var value3 = new StringWithQualityHeaderValue("T", 0.123);
        var value4 = new StringWithQualityHeaderValue("t");
        var value5 = new StringWithQualityHeaderValue("x", 0.123);
        var value6 = new StringWithQualityHeaderValue("t", 0.5);
        var value7 = new StringWithQualityHeaderValue("t", 0.1234);
        var value8 = new StringWithQualityHeaderValue("T");
        var value9 = new StringWithQualityHeaderValue("x");

        Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
        Assert.Equal(value1.GetHashCode(), value3.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value4.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value5.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value6.GetHashCode());
        Assert.NotEqual(value1.GetHashCode(), value7.GetHashCode());
        Assert.Equal(value4.GetHashCode(), value8.GetHashCode());
        Assert.NotEqual(value4.GetHashCode(), value9.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
    {
        var value1 = new StringWithQualityHeaderValue("t", 0.123);
        var value2 = new StringWithQualityHeaderValue("t", 0.123);
        var value3 = new StringWithQualityHeaderValue("T", 0.123);
        var value4 = new StringWithQualityHeaderValue("t");
        var value5 = new StringWithQualityHeaderValue("x", 0.123);
        var value6 = new StringWithQualityHeaderValue("t", 0.5);
        var value7 = new StringWithQualityHeaderValue("t", 0.1234);
        var value8 = new StringWithQualityHeaderValue("T");
        var value9 = new StringWithQualityHeaderValue("x");

        Assert.False(value1.Equals(null), "t; q=0.123 vs. <null>");
        Assert.True(value1!.Equals(value2), "t; q=0.123 vs. t; q=0.123");
        Assert.True(value1.Equals(value3), "t; q=0.123 vs. T; q=0.123");
        Assert.False(value1.Equals(value4), "t; q=0.123 vs. t");
        Assert.False(value4.Equals(value1), "t vs. t; q=0.123");
        Assert.False(value1.Equals(value5), "t; q=0.123 vs. x; q=0.123");
        Assert.False(value1.Equals(value6), "t; q=0.123 vs. t; q=0.5");
        Assert.False(value1.Equals(value7), "t; q=0.123 vs. t; q=0.1234");
        Assert.True(value4.Equals(value8), "t vs. T");
        Assert.False(value4.Equals(value9), "t vs. T");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse("text", new StringWithQualityHeaderValue("text"));
        CheckValidParse("text;q=0.5", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidParse("text ; q = 0.5", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidParse("\r\n text ; q = 0.5 ", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidParse("  text  ", new StringWithQualityHeaderValue("text"));
        CheckValidParse(" \r\n text \r\n ; \r\n q = 0.123", new StringWithQualityHeaderValue("text", 0.123));
        CheckValidParse(" text ; q = 0.123 ", new StringWithQualityHeaderValue("text", 0.123));
        CheckValidParse("text;q=1 ", new StringWithQualityHeaderValue("text", 1));
        CheckValidParse("*", new StringWithQualityHeaderValue("*"));
        CheckValidParse("*;q=0.7", new StringWithQualityHeaderValue("*", 0.7));
        CheckValidParse(" t", new StringWithQualityHeaderValue("t"));
        CheckValidParse("t;q=0.", new StringWithQualityHeaderValue("t", 0));
        CheckValidParse("t;q=1.", new StringWithQualityHeaderValue("t", 1));
        CheckValidParse("t;q=1.000", new StringWithQualityHeaderValue("t", 1));
        CheckValidParse("t;q=0.12345678", new StringWithQualityHeaderValue("t", 0.12345678));
        CheckValidParse("t ;  q  =   0", new StringWithQualityHeaderValue("t", 0));
        CheckValidParse("iso-8859-5", new StringWithQualityHeaderValue("iso-8859-5"));
        CheckValidParse("unicode-1-1; q=0.8", new StringWithQualityHeaderValue("unicode-1-1", 0.8));
    }

    [Theory]
    [InlineData("text,")]
    [InlineData("\r\n text ; q = 0.5, next_text  ")]
    [InlineData("  text,next_text  ")]
    [InlineData(" ,, text, , ,next")]
    [InlineData(" ,, text, , ,")]
    [InlineData(", \r\n text \r\n ; \r\n q = 0.123")]
    [InlineData("teäxt")]
    [InlineData("text会")]
    [InlineData("会")]
    [InlineData("t;q=会")]
    [InlineData("t;q=")]
    [InlineData("t;q")]
    [InlineData("t;会=1")]
    [InlineData("t;q会=1")]
    [InlineData("t y")]
    [InlineData("t;q=1 y")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("  ,,")]
    [InlineData("t;q=-1")]
    [InlineData("t;q=1.00001")]
    [InlineData("t;")]
    [InlineData("t;;q=1")]
    [InlineData("t;q=a")]
    [InlineData("t;qa")]
    [InlineData("t;q1")]
    [InlineData("integer_part_too_long;q=01")]
    [InlineData("integer_part_too_long;q=01.0")]
    [InlineData("decimal_part_too_long;q=0.123456789")]
    [InlineData("decimal_part_too_long;q=0.123456789 ")]
    [InlineData("no_integer_part;q=.1")]
    public void Parse_SetOfInvalidValueStrings_Throws(string input)
    {
        Assert.Throws<FormatException>(() => StringWithQualityHeaderValue.Parse(input));
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse("text", new StringWithQualityHeaderValue("text"));
        CheckValidTryParse("text;q=0.5", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidTryParse("text ; q = 0.5", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidTryParse("\r\n text ; q = 0.5 ", new StringWithQualityHeaderValue("text", 0.5));
        CheckValidTryParse("  text  ", new StringWithQualityHeaderValue("text"));
        CheckValidTryParse(" \r\n text \r\n ; \r\n q = 0.123", new StringWithQualityHeaderValue("text", 0.123));
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse("text,");
        CheckInvalidTryParse("\r\n text ; q = 0.5, next_text  ");
        CheckInvalidTryParse("  text,next_text  ");
        CheckInvalidTryParse(" ,, text, , ,next");
        CheckInvalidTryParse(" ,, text, , ,");
        CheckInvalidTryParse(", \r\n text \r\n ; \r\n q = 0.123");
        CheckInvalidTryParse("teäxt");
        CheckInvalidTryParse("text会");
        CheckInvalidTryParse("会");
        CheckInvalidTryParse("t;q=会");
        CheckInvalidTryParse("t;q=");
        CheckInvalidTryParse("t;q");
        CheckInvalidTryParse("t;会=1");
        CheckInvalidTryParse("t;q会=1");
        CheckInvalidTryParse("t y");
        CheckInvalidTryParse("t;q=1 y");

        CheckInvalidTryParse(null);
        CheckInvalidTryParse(string.Empty);
        CheckInvalidTryParse("  ");
        CheckInvalidTryParse("  ,,");
    }

    [Fact]
    public void ParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text2,",
                "textA,textB",
                "text3;q=0.5",
                "text4;q=0.5,",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        IList<StringWithQualityHeaderValue> results = StringWithQualityHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("textA"),
                new StringWithQualityHeaderValue("textB"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text2,",
                "textA,textB",
                "text3;q=0.5",
                "text4;q=0.5,",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        IList<StringWithQualityHeaderValue> results = StringWithQualityHeaderValue.ParseStrictList(inputs);

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("textA"),
                new StringWithQualityHeaderValue("textB"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text2,",
                "textA,textB",
                "text3;q=0.5",
                "text4;q=0.5,",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        Assert.True(StringWithQualityHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("textA"),
                new StringWithQualityHeaderValue("textB"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text2,",
                "textA,textB",
                "text3;q=0.5",
                "text4;q=0.5,",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        Assert.True(StringWithQualityHeaderValue.TryParseStrictList(inputs, out var results));

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("textA"),
                new StringWithQualityHeaderValue("textB"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseList_WithSomeInvalidValues_IgnoresInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text 1",
                "text2",
                "\"text 2\",",
                "text3;q=0.5",
                "text4;q=0.5, extra stuff",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        var results = StringWithQualityHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("stuff"),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_WithSomeInvalidValues_Throws()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text 1",
                "text2",
                "\"text 2\",",
                "text3;q=0.5",
                "text4;q=0.5, extra stuff",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        Assert.Throws<FormatException>(() => StringWithQualityHeaderValue.ParseStrictList(inputs));
    }

    [Fact]
    public void TryParseList_WithSomeInvalidValues_IgnoresInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text 1",
                "text2",
                "\"text 2\",",
                "text3;q=0.5",
                "text4;q=0.5, extra stuff",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        Assert.True(StringWithQualityHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new StringWithQualityHeaderValue("text1"),
                new StringWithQualityHeaderValue("1"),
                new StringWithQualityHeaderValue("text2"),
                new StringWithQualityHeaderValue("text3", 0.5),
                new StringWithQualityHeaderValue("text4", 0.5),
                new StringWithQualityHeaderValue("stuff"),
                new StringWithQualityHeaderValue("text5", 0.5),
                new StringWithQualityHeaderValue("text6", 0.05),
                new StringWithQualityHeaderValue("text7"),
                new StringWithQualityHeaderValue("text8", 0.5),
                new StringWithQualityHeaderValue("text9"),
                new StringWithQualityHeaderValue("text10", 0.5),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_WithSomeInvalidValues_ReturnsFalse()
    {
        var inputs = new[]
        {
                "",
                "text1",
                "text 1",
                "text2",
                "\"text 2\",",
                "text3;q=0.5",
                "text4;q=0.5, extra stuff",
                " text5 ; q = 0.50 ",
                "\r\n text6 ; q = 0.05 ",
                "text7,text8;q=0.5",
                " text9 , text10 ; q = 0.5 ",
            };
        Assert.False(StringWithQualityHeaderValue.TryParseStrictList(inputs, out var results));
    }

    #region Helper methods

    private void CheckValidParse(string? input, StringWithQualityHeaderValue expectedResult)
    {
        var result = StringWithQualityHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckValidTryParse(string? input, StringWithQualityHeaderValue expectedResult)
    {
        Assert.True(StringWithQualityHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(StringWithQualityHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    #endregion
}
