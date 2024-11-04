// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class NameValueHeaderValueTest
{
    [Fact]
    public void Ctor_NameNull_Throw()
    {
        Assert.Throws<ArgumentException>(() => new NameValueHeaderValue(null));
        // null and empty should be treated the same. So we also throw for empty strings.
        Assert.Throws<ArgumentException>(() => new NameValueHeaderValue(string.Empty));
    }

    [Fact]
    public void Ctor_NameInvalidFormat_ThrowFormatException()
    {
        // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
        AssertFormatException(" text ", null);
        AssertFormatException("text ", null);
        AssertFormatException(" text", null);
        AssertFormatException("te xt", null);
        AssertFormatException("te=xt", null); // The ctor takes a name which must not contain '='.
        AssertFormatException("teäxt", null);
    }

    [Fact]
    public void Ctor_NameValidFormat_SuccessfullyCreated()
    {
        var nameValue = new NameValueHeaderValue("text", null);
        Assert.Equal("text", nameValue.Name);
    }

    [Fact]
    public void Ctor_ValueInvalidFormat_ThrowFormatException()
    {
        // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
        AssertFormatException("text", " token ");
        AssertFormatException("text", "token ");
        AssertFormatException("text", " token");
        AssertFormatException("text", "token string");
        AssertFormatException("text", "\"quoted string with \" quotes\"");
        AssertFormatException("text", "\"quoted string with \"two\" quotes\"");
    }

    [Fact]
    public void Ctor_ValueValidFormat_SuccessfullyCreated()
    {
        CheckValue(null);
        CheckValue(string.Empty);
        CheckValue("token_string");
        CheckValue("\"quoted string\"");
        CheckValue("\"quoted string with quoted \\\" quote-pair\"");
    }

    [Fact]
    public void Copy_NameOnly_SuccessfullyCopied()
    {
        var pair0 = new NameValueHeaderValue("name");
        var pair1 = pair0.Copy();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Null(pair0.Value.Value);
        Assert.Null(pair1.Value.Value);

        // Change one value and verify the other is unchanged.
        pair0.Value = "othervalue";
        Assert.Equal("othervalue", pair0.Value);
        Assert.Null(pair1.Value.Value);
    }

    [Fact]
    public void CopyAsReadOnly_NameOnly_CopiedAndReadOnly()
    {
        var pair0 = new NameValueHeaderValue("name");
        var pair1 = pair0.CopyAsReadOnly();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Null(pair0.Value.Value);
        Assert.Null(pair1.Value.Value);
        Assert.False(pair0.IsReadOnly);
        Assert.True(pair1.IsReadOnly);

        // Change one value and verify the other is unchanged.
        pair0.Value = "othervalue";
        Assert.Equal("othervalue", pair0.Value);
        Assert.Null(pair1.Value.Value);
        Assert.Throws<InvalidOperationException>(() => { pair1.Value = "othervalue"; });
    }

    [Fact]
    public void Copy_NameAndValue_SuccessfullyCopied()
    {
        var pair0 = new NameValueHeaderValue("name", "value");
        var pair1 = pair0.Copy();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Same(pair0.Value.Value, pair1.Value.Value);

        // Change one value and verify the other is unchanged.
        pair0.Value = "othervalue";
        Assert.Equal("othervalue", pair0.Value);
        Assert.Equal("value", pair1.Value);
    }

    [Fact]
    public void CopyAsReadOnly_NameAndValue_CopiedAndReadOnly()
    {
        var pair0 = new NameValueHeaderValue("name", "value");
        var pair1 = pair0.CopyAsReadOnly();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Same(pair0.Value.Value, pair1.Value.Value);
        Assert.False(pair0.IsReadOnly);
        Assert.True(pair1.IsReadOnly);

        // Change one value and verify the other is unchanged.
        pair0.Value = "othervalue";
        Assert.Equal("othervalue", pair0.Value);
        Assert.Equal("value", pair1.Value);
        Assert.Throws<InvalidOperationException>(() => { pair1.Value = "othervalue"; });
    }

    [Fact]
    public void CopyFromReadOnly_NameAndValue_CopiedAsNonReadOnly()
    {
        var pair0 = new NameValueHeaderValue("name", "value");
        var pair1 = pair0.CopyAsReadOnly();
        var pair2 = pair1.Copy();
        Assert.NotSame(pair0, pair1);
        Assert.Same(pair0.Name.Value, pair1.Name.Value);
        Assert.Same(pair0.Value.Value, pair1.Value.Value);

        // Change one value and verify the other is unchanged.
        pair2.Value = "othervalue";
        Assert.Equal("othervalue", pair2.Value);
        Assert.Equal("value", pair1.Value);
    }

    [Fact]
    public void Value_CallSetterWithInvalidValues_Throw()
    {
        // Just verify that the setter calls the same validation the ctor invokes.
        Assert.Throws<FormatException>(() => { var x = new NameValueHeaderValue("name"); x.Value = " x "; });
        Assert.Throws<FormatException>(() => { var x = new NameValueHeaderValue("name"); x.Value = "x y"; });
    }

    [Fact]
    public void ToString_UseNoValueAndTokenAndQuotedStringValues_SerializedCorrectly()
    {
        var nameValue = new NameValueHeaderValue("text", "token");
        Assert.Equal("text=token", nameValue.ToString());

        nameValue.Value = "\"quoted string\"";
        Assert.Equal("text=\"quoted string\"", nameValue.ToString());

        nameValue.Value = null;
        Assert.Equal("text", nameValue.ToString());

        nameValue.Value = string.Empty;
        Assert.Equal("text", nameValue.ToString());
    }

    [Fact]
    public void GetHashCode_ValuesUseDifferentValues_HashDiffersAccordingToRfc()
    {
        var nameValue1 = new NameValueHeaderValue("text");
        var nameValue2 = new NameValueHeaderValue("text");

        nameValue1.Value = null;
        nameValue2.Value = null;
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "token";
        nameValue2.Value = null;
        Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "token";
        nameValue2.Value = string.Empty;
        Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = null;
        nameValue2.Value = string.Empty;
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "token";
        nameValue2.Value = "TOKEN";
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "token";
        nameValue2.Value = "token";
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "\"quoted string\"";
        nameValue2.Value = "\"QUOTED STRING\"";
        Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

        nameValue1.Value = "\"quoted string\"";
        nameValue2.Value = "\"quoted string\"";
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_NameUseDifferentCasing_HashDiffersAccordingToRfc()
    {
        var nameValue1 = new NameValueHeaderValue("text");
        var nameValue2 = new NameValueHeaderValue("TEXT");
        Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());
    }

    [Fact]
    public void Equals_ValuesUseDifferentValues_ValuesAreEqualOrDifferentAccordingToRfc()
    {
        var nameValue1 = new NameValueHeaderValue("text");
        var nameValue2 = new NameValueHeaderValue("text");

        nameValue1.Value = null;
        nameValue2.Value = null;
        Assert.True(nameValue1.Equals(nameValue2), "<null> vs. <null>.");

        nameValue1.Value = "token";
        nameValue2.Value = null;
        Assert.False(nameValue1.Equals(nameValue2), "token vs. <null>.");

        nameValue1.Value = null;
        nameValue2.Value = "token";
        Assert.False(nameValue1.Equals(nameValue2), "<null> vs. token.");

        nameValue1.Value = string.Empty;
        nameValue2.Value = "token";
        Assert.False(nameValue1.Equals(nameValue2), "string.Empty vs. token.");

        nameValue1.Value = null;
        nameValue2.Value = string.Empty;
        Assert.True(nameValue1.Equals(nameValue2), "<null> vs. string.Empty.");

        nameValue1.Value = "token";
        nameValue2.Value = "TOKEN";
        Assert.True(nameValue1.Equals(nameValue2), "token vs. TOKEN.");

        nameValue1.Value = "token";
        nameValue2.Value = "token";
        Assert.True(nameValue1.Equals(nameValue2), "token vs. token.");

        nameValue1.Value = "\"quoted string\"";
        nameValue2.Value = "\"QUOTED STRING\"";
        Assert.False(nameValue1.Equals(nameValue2), "\"quoted string\" vs. \"QUOTED STRING\".");

        nameValue1.Value = "\"quoted string\"";
        nameValue2.Value = "\"quoted string\"";
        Assert.True(nameValue1.Equals(nameValue2), "\"quoted string\" vs. \"quoted string\".");

        Assert.False(nameValue1.Equals(null), "\"quoted string\" vs. <null>.");
    }

    [Fact]
    public void Equals_NameUseDifferentCasing_ConsideredEqual()
    {
        var nameValue1 = new NameValueHeaderValue("text");
        var nameValue2 = new NameValueHeaderValue("TEXT");
        Assert.True(nameValue1.Equals(nameValue2), "text vs. TEXT.");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse("  name = value    ", new NameValueHeaderValue("name", "value"));
        CheckValidParse(" name", new NameValueHeaderValue("name"));
        CheckValidParse(" name   ", new NameValueHeaderValue("name"));
        CheckValidParse(" name=\"value\"", new NameValueHeaderValue("name", "\"value\""));
        CheckValidParse("name=value", new NameValueHeaderValue("name", "value"));
        CheckValidParse("name=\"quoted str\"", new NameValueHeaderValue("name", "\"quoted str\""));
        CheckValidParse("name\t =va1ue", new NameValueHeaderValue("name", "va1ue"));
        CheckValidParse("name= va*ue ", new NameValueHeaderValue("name", "va*ue"));
        CheckValidParse("name=", new NameValueHeaderValue("name", ""));
    }

    [Fact]
    public void Parse_SetOfInvalidValueStrings_Throws()
    {
        CheckInvalidParse("name[value");
        CheckInvalidParse("name=value=");
        CheckInvalidParse("name=会");
        CheckInvalidParse("name==value");
        CheckInvalidParse("name= va:ue");
        CheckInvalidParse("=value");
        CheckInvalidParse("name value");
        CheckInvalidParse("name=,value");
        CheckInvalidParse("会");
        CheckInvalidParse(null);
        CheckInvalidParse(string.Empty);
        CheckInvalidParse("  ");
        CheckInvalidParse("  ,,");
        CheckInvalidParse(" , , name = value  ,  ");
        CheckInvalidParse(" name,");
        CheckInvalidParse(" ,name=\"value\"");
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse("  name = value    ", new NameValueHeaderValue("name", "value"));
        CheckValidTryParse(" name", new NameValueHeaderValue("name"));
        CheckValidTryParse(" name=\"value\"", new NameValueHeaderValue("name", "\"value\""));
        CheckValidTryParse("name=value", new NameValueHeaderValue("name", "value"));
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse("name[value");
        CheckInvalidTryParse("name=value=");
        CheckInvalidTryParse("name=会");
        CheckInvalidTryParse("name==value");
        CheckInvalidTryParse("=value");
        CheckInvalidTryParse("name value");
        CheckInvalidTryParse("name=,value");
        CheckInvalidTryParse("会");
        CheckInvalidTryParse(null);
        CheckInvalidTryParse(string.Empty);
        CheckInvalidTryParse("  ");
        CheckInvalidTryParse("  ,,");
        CheckInvalidTryParse(" , , name = value  ,  ");
        CheckInvalidTryParse(" name,");
        CheckInvalidTryParse(" ,name=\"value\"");
    }

    [Fact]
    public void ParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "name=value1",
                "",
                " name = value2 ",
                "\r\n name =value3\r\n ",
                "name=\"value 4\"",
                "name=\"value会5\"",
                "name=value6,name=value7",
                "name=\"value 8\", name= \"value 9\"",
            };
        var results = NameValueHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name", "value1"),
                new NameValueHeaderValue("name", "value2"),
                new NameValueHeaderValue("name", "value3"),
                new NameValueHeaderValue("name", "\"value 4\""),
                new NameValueHeaderValue("name", "\"value会5\""),
                new NameValueHeaderValue("name", "value6"),
                new NameValueHeaderValue("name", "value7"),
                new NameValueHeaderValue("name", "\"value 8\""),
                new NameValueHeaderValue("name", "\"value 9\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "name=value1",
                "",
                " name = value2 ",
                "\r\n name =value3\r\n ",
                "name=\"value 4\"",
                "name=\"value会5\"",
                "name=value6,name=value7",
                "name=\"value 8\", name= \"value 9\"",
            };
        var results = NameValueHeaderValue.ParseStrictList(inputs);

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name", "value1"),
                new NameValueHeaderValue("name", "value2"),
                new NameValueHeaderValue("name", "value3"),
                new NameValueHeaderValue("name", "\"value 4\""),
                new NameValueHeaderValue("name", "\"value会5\""),
                new NameValueHeaderValue("name", "value6"),
                new NameValueHeaderValue("name", "value7"),
                new NameValueHeaderValue("name", "\"value 8\""),
                new NameValueHeaderValue("name", "\"value 9\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "name=value1",
                "",
                " name = value2 ",
                "\r\n name =value3\r\n ",
                "name=\"value 4\"",
                "name=\"value会5\"",
                "name=value6,name=value7",
                "name=\"value 8\", name= \"value 9\"",
            };
        Assert.True(NameValueHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name", "value1"),
                new NameValueHeaderValue("name", "value2"),
                new NameValueHeaderValue("name", "value3"),
                new NameValueHeaderValue("name", "\"value 4\""),
                new NameValueHeaderValue("name", "\"value会5\""),
                new NameValueHeaderValue("name", "value6"),
                new NameValueHeaderValue("name", "value7"),
                new NameValueHeaderValue("name", "\"value 8\""),
                new NameValueHeaderValue("name", "\"value 9\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_SetOfValidValueStrings_ParsedCorrectly()
    {
        var inputs = new[]
        {
                "",
                "name=value1",
                "",
                " name = value2 ",
                "\r\n name =value3\r\n ",
                "name=\"value 4\"",
                "name=\"value会5\"",
                "name=value6,name=value7",
                "name=\"value 8\", name= \"value 9\"",
            };
        Assert.True(NameValueHeaderValue.TryParseStrictList(inputs, out var results));

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name", "value1"),
                new NameValueHeaderValue("name", "value2"),
                new NameValueHeaderValue("name", "value3"),
                new NameValueHeaderValue("name", "\"value 4\""),
                new NameValueHeaderValue("name", "\"value会5\""),
                new NameValueHeaderValue("name", "value6"),
                new NameValueHeaderValue("name", "value7"),
                new NameValueHeaderValue("name", "\"value 8\""),
                new NameValueHeaderValue("name", "\"value 9\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseList_WithSomeInvalidValues_ExcludesInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "name1=value1",
                "name2",
                " name3 = 3, value a",
                "name4 =value4, name5 = value5 b",
                "name6=\"value 6",
                "name7=\"value会7\"",
                "name8=value8,name9=value9",
                "name10=\"value 10\", name11= \"value 11\"",
            };
        var results = NameValueHeaderValue.ParseList(inputs);

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name1", "value1"),
                new NameValueHeaderValue("name2"),
                new NameValueHeaderValue("name3", "3"),
                new NameValueHeaderValue("a"),
                new NameValueHeaderValue("name4", "value4"),
                new NameValueHeaderValue("b"),
                new NameValueHeaderValue("6"),
                new NameValueHeaderValue("name7", "\"value会7\""),
                new NameValueHeaderValue("name8", "value8"),
                new NameValueHeaderValue("name9", "value9"),
                new NameValueHeaderValue("name10", "\"value 10\""),
                new NameValueHeaderValue("name11", "\"value 11\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void ParseStrictList_WithSomeInvalidValues_Throws()
    {
        var inputs = new[]
        {
                "",
                "name1=value1",
                "name2",
                " name3 = 3, value a",
                "name4 =value4, name5 = value5 b",
                "name6=\"value 6",
                "name7=\"value会7\"",
                "name8=value8,name9=value9",
                "name10=\"value 10\", name11= \"value 11\"",
            };
        Assert.Throws<FormatException>(() => NameValueHeaderValue.ParseStrictList(inputs));
    }

    [Fact]
    public void TryParseList_WithSomeInvalidValues_ExcludesInvalidValues()
    {
        var inputs = new[]
        {
                "",
                "name1=value1",
                "name2",
                " name3 = 3, value a",
                "name4 =value4, name5 = value5 b",
                "name6=\"value 6",
                "name7=\"value会7\"",
                "name8=value8,name9=value9",
                "name10=\"value 10\", name11= \"value 11\"",
            };
        Assert.True(NameValueHeaderValue.TryParseList(inputs, out var results));

        var expectedResults = new[]
        {
                new NameValueHeaderValue("name1", "value1"),
                new NameValueHeaderValue("name2"),
                new NameValueHeaderValue("name3", "3"),
                new NameValueHeaderValue("a"),
                new NameValueHeaderValue("name4", "value4"),
                new NameValueHeaderValue("b"),
                new NameValueHeaderValue("6"),
                new NameValueHeaderValue("name7", "\"value会7\""),
                new NameValueHeaderValue("name8", "value8"),
                new NameValueHeaderValue("name9", "value9"),
                new NameValueHeaderValue("name10", "\"value 10\""),
                new NameValueHeaderValue("name11", "\"value 11\""),
            }.ToList();

        Assert.Equal(expectedResults, results);
    }

    [Fact]
    public void TryParseStrictList_WithSomeInvalidValues_ReturnsFalse()
    {
        var inputs = new[]
        {
                "",
                "name1=value1",
                "name2",
                " name3 = 3, value a",
                "name4 =value4, name5 = value5 b",
                "name6=\"value 6",
                "name7=\"value会7\"",
                "name8=value8,name9=value9",
                "name10=\"value 10\", name11= \"value 11\"",
            };
        Assert.False(NameValueHeaderValue.TryParseStrictList(inputs, out var results));
    }

    [Theory]
    [InlineData("value", "value")]
    [InlineData("\"value\"", "value")]
    [InlineData("\"hello\\\\\"", "hello\\")]
    [InlineData("\"hello\\\"\"", "hello\"")]
    [InlineData("\"hello\\\"foo\\\\bar\\\\baz\\\\\"", "hello\"foo\\bar\\baz\\")]
    [InlineData("\"quoted value\"", "quoted value")]
    [InlineData("\"quoted\\\"valuewithquote\"", "quoted\"valuewithquote")]
    [InlineData("\"hello\\\"", "hello\\")]
    public void GetUnescapedValue_ReturnsExpectedValue(string input, string expected)
    {
        var header = new NameValueHeaderValue("test", input);

        var actual = header.GetUnescapedValue();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("value", "value")]
    [InlineData("23", "23")]
    [InlineData(";;;", "\";;;\"")]
    [InlineData("\"value\"", "\"value\"")]
    [InlineData("\"assumes already encoded \\\"\"", "\"assumes already encoded \\\"\"")]
    [InlineData("unquoted \"value", "\"unquoted \\\"value\"")]
    [InlineData("value\\morevalues\\evenmorevalues", "\"value\\\\morevalues\\\\evenmorevalues\"")]
    // We have to assume that the input needs to be quoted here
    [InlineData("\"\"double quoted string\"\"", "\"\\\"\\\"double quoted string\\\"\\\"\"")]
    [InlineData("\t", "\"\t\"")]
    public void SetAndEscapeValue_ReturnsExpectedValue(string input, string expected)
    {
        var header = new NameValueHeaderValue("test");
        header.SetAndEscapeValue(input);

        var actual = header.Value;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\b")]
    [InlineData("\r")]
    public void SetAndEscapeValue_ThrowsOnInvalidValues(string input)
    {
        var header = new NameValueHeaderValue("test");
        Assert.Throws<FormatException>(() => header.SetAndEscapeValue(input));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("\"value\\\\morevalues\\\\evenmorevalues\"")]
    [InlineData("\"quoted \\\"value\"")]
    public void GetAndSetEncodeValueRoundTrip_ReturnsExpectedValue(string input)
    {
        var header = new NameValueHeaderValue("test");
        header.Value = input;
        var valueHeader = header.GetUnescapedValue();
        header.SetAndEscapeValue(valueHeader);

        var actual = header.Value;

        Assert.Equal(input, actual);
    }

    [Theory]
    [InlineData("val\\nue")]
    [InlineData("val\\bue")]
    public void OverescapingValuesDoNotRoundTrip(string input)
    {
        var header = new NameValueHeaderValue("test");
        header.SetAndEscapeValue(input);
        var valueHeader = header.GetUnescapedValue();

        var actual = header.Value;

        Assert.NotEqual(input, actual);
    }

    #region Helper methods

    private void CheckValidParse(string? input, NameValueHeaderValue expectedResult)
    {
        var result = NameValueHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidParse(string? input)
    {
        Assert.Throws<FormatException>(() => NameValueHeaderValue.Parse(input));
    }

    private void CheckValidTryParse(string? input, NameValueHeaderValue expectedResult)
    {
        Assert.True(NameValueHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(NameValueHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    private static void CheckValue(string? value)
    {
        var nameValue = new NameValueHeaderValue("text", value);
        Assert.Equal(value, nameValue.Value);
    }

    private static void AssertFormatException(string name, string? value)
    {
        Assert.Throws<FormatException>(() => new NameValueHeaderValue(name, value));
    }

    #endregion
}
