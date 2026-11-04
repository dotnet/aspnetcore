// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class ContentRangeHeaderValueTest
{
    [Fact]
    public void Ctor_LengthOnlyOverloadUseInvalidValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(-1));
    }

    [Fact]
    public void Ctor_LengthOnlyOverloadValidValues_ValuesCorrectlySet()
    {
        var range = new ContentRangeHeaderValue(5);

        Assert.False(range.HasRange, "HasRange");
        Assert.True(range.HasLength, "HasLength");
        Assert.Equal("bytes", range.Unit.AsSpan());
        Assert.Null(range.From);
        Assert.Null(range.To);
        Assert.Equal(5, range.Length);
    }

    [Fact]
    public void Ctor_FromAndToOverloadUseInvalidValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(2, 1));
    }

    [Fact]
    public void Ctor_FromAndToOverloadValidValues_ValuesCorrectlySet()
    {
        var range = new ContentRangeHeaderValue(0, 1);

        Assert.True(range.HasRange, "HasRange");
        Assert.False(range.HasLength, "HasLength");
        Assert.Equal("bytes", range.Unit.AsSpan());
        Assert.Equal(0, range.From);
        Assert.Equal(1, range.To);
        Assert.Null(range.Length);
    }

    [Fact]
    public void Ctor_FromToAndLengthOverloadUseInvalidValues_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(-1, 1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(0, -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(0, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(2, 1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(1, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentRangeHeaderValue(1, 2, 2));
    }

    [Fact]
    public void Ctor_FromToAndLengthOverloadValidValues_ValuesCorrectlySet()
    {
        var range = new ContentRangeHeaderValue(0, 1, 2);

        Assert.True(range.HasRange, "HasRange");
        Assert.True(range.HasLength, "HasLength");
        Assert.Equal("bytes", range.Unit.AsSpan());
        Assert.Equal(0, range.From);
        Assert.Equal(1, range.To);
        Assert.Equal(2, range.Length);
    }

    [Fact]
    public void Unit_GetAndSetValidAndInvalidValues_MatchExpectation()
    {
        var range = new ContentRangeHeaderValue(0);
        range.Unit = "myunit";
        Assert.Equal("myunit", range.Unit.AsSpan());

        Assert.Throws<ArgumentException>(() => range.Unit = null);
        Assert.Throws<ArgumentException>(() => range.Unit = "");
        Assert.Throws<FormatException>(() => range.Unit = " x");
        Assert.Throws<FormatException>(() => range.Unit = "x ");
        Assert.Throws<FormatException>(() => range.Unit = "x y");
    }

    [Fact]
    public void ToString_UseDifferentRanges_AllSerializedCorrectly()
    {
        var range = new ContentRangeHeaderValue(1, 2, 3);
        range.Unit = "myunit";
        Assert.Equal("myunit 1-2/3", range.ToString());

        range = new ContentRangeHeaderValue(123456789012345678, 123456789012345679);
        Assert.Equal("bytes 123456789012345678-123456789012345679/*", range.ToString());

        range = new ContentRangeHeaderValue(150);
        Assert.Equal("bytes */150", range.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
    {
        var range1 = new ContentRangeHeaderValue(1, 2, 5);
        var range2 = new ContentRangeHeaderValue(1, 2);
        var range3 = new ContentRangeHeaderValue(5);
        var range4 = new ContentRangeHeaderValue(1, 2, 5);
        range4.Unit = "BYTES";
        var range5 = new ContentRangeHeaderValue(1, 2, 5);
        range5.Unit = "myunit";

        Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode());
        Assert.NotEqual(range1.GetHashCode(), range3.GetHashCode());
        Assert.NotEqual(range2.GetHashCode(), range3.GetHashCode());
        Assert.Equal(range1.GetHashCode(), range4.GetHashCode());
        Assert.NotEqual(range1.GetHashCode(), range5.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
    {
        var range1 = new ContentRangeHeaderValue(1, 2, 5);
        var range2 = new ContentRangeHeaderValue(1, 2);
        var range3 = new ContentRangeHeaderValue(5);
        var range4 = new ContentRangeHeaderValue(1, 2, 5);
        range4.Unit = "BYTES";
        var range5 = new ContentRangeHeaderValue(1, 2, 5);
        range5.Unit = "myunit";
        var range6 = new ContentRangeHeaderValue(1, 3, 5);
        var range7 = new ContentRangeHeaderValue(2, 2, 5);
        var range8 = new ContentRangeHeaderValue(1, 2, 6);

        Assert.False(range1.Equals(null), "bytes 1-2/5 vs. <null>");
        Assert.False(range1!.Equals(range2), "bytes 1-2/5 vs. bytes 1-2/*");
        Assert.False(range1.Equals(range3), "bytes 1-2/5 vs. bytes */5");
        Assert.False(range2.Equals(range3), "bytes 1-2/* vs. bytes */5");
        Assert.True(range1.Equals(range4), "bytes 1-2/5 vs. BYTES 1-2/5");
        Assert.True(range4.Equals(range1), "BYTES 1-2/5 vs. bytes 1-2/5");
        Assert.False(range1.Equals(range5), "bytes 1-2/5 vs. myunit 1-2/5");
        Assert.False(range1.Equals(range6), "bytes 1-2/5 vs. bytes 1-3/5");
        Assert.False(range1.Equals(range7), "bytes 1-2/5 vs. bytes 2-2/5");
        Assert.False(range1.Equals(range8), "bytes 1-2/5 vs. bytes 1-2/6");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse(" bytes 1-2/3 ", new ContentRangeHeaderValue(1, 2, 3));
        CheckValidParse("bytes  *  /  3", new ContentRangeHeaderValue(3));

        CheckValidParse(" custom 1234567890123456789-1234567890123456799/*",
            new ContentRangeHeaderValue(1234567890123456789, 1234567890123456799) { Unit = "custom" });

        CheckValidParse(" custom * / 123 ",
            new ContentRangeHeaderValue(123) { Unit = "custom" });

        // Note that we don't have a public constructor for value 'bytes */*' since the RFC doesn't mention a
        // scenario for it. However, if a server returns this value, we're flexible and accept it.
        var result = ContentRangeHeaderValue.Parse("bytes */*");
        Assert.Equal("bytes", result.Unit.AsSpan());
        Assert.Null(result.From);
        Assert.Null(result.To);
        Assert.Null(result.Length);
        Assert.False(result.HasRange, "HasRange");
        Assert.False(result.HasLength, "HasLength");
    }

    [Theory]
    [MemberData(nameof(InvalidContentRangeValueStrings))]
    [InlineData(null)]
    public void Parse_SetOfInvalidValueStrings_Throws(string? input)
    {
        Assert.Throws<FormatException>(() => ContentRangeHeaderValue.Parse(input));
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse(" bytes 1-2/3 ", new ContentRangeHeaderValue(1, 2, 3));
        CheckValidTryParse("bytes  *  /  3", new ContentRangeHeaderValue(3));

        CheckValidTryParse(" custom 1234567890123456789-1234567890123456799/*",
            new ContentRangeHeaderValue(1234567890123456789, 1234567890123456799) { Unit = "custom" });

        CheckValidTryParse(" custom * / 123 ",
            new ContentRangeHeaderValue(123) { Unit = "custom" });

        // Note that we don't have a public constructor for value 'bytes */*' since the RFC doesn't mention a
        // scenario for it. However, if a server returns this value, we're flexible and accept it.
        Assert.True(ContentRangeHeaderValue.TryParse("bytes */*", out var result));
        Assert.Equal("bytes", result.Unit.AsSpan());
        Assert.Null(result.From);
        Assert.Null(result.To);
        Assert.Null(result.Length);
        Assert.False(result.HasRange, "HasRange");
        Assert.False(result.HasLength, "HasLength");
    }

    [Theory]
    [MemberData(nameof(InvalidContentRangeValueStrings))]
    [InlineData(null)]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse(string? input)
    {
        Assert.False(ContentRangeHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    private static void CheckValidParse(string? input, ContentRangeHeaderValue expectedResult)
    {
        var result = ContentRangeHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private static void CheckValidTryParse(string? input, ContentRangeHeaderValue expectedResult)
    {
        Assert.True(ContentRangeHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    public static TheoryData<string> InvalidContentRangeValueStrings =>
        new TheoryData<string>
        {
            {"bytes 1-2/3,"}, // no character after 'length' allowed
            {"x bytes 1-2/3"},
            {"bytes 1-2/3.4"},
            {""},
            {"bytes 3-2/5"},
            {"bytes 6-6/5"},
            {"bytes 1-6/5"},
            {"bytes 1-2/"},
            {"bytes 1-2"},
            {"bytes 1-/"},
            {"bytes 1-"},
            {"bytes 1"},
            {"bytes "},
            {"bytes a-2/3"},
            {"bytes 1-b/3"},
            {"bytes 1-2/c"},
            {"bytes1-2/3"},
            {"bytes 0-0/0"},
            // More than 19 digits >>Int64.MaxValue
            {"bytes 1-12345678901234567890/3"},
            {"bytes 12345678901234567890-3/3"},
            {"bytes 1-2/12345678901234567890"},
            // Exceed Int64.MaxValue, but use 19 digits
            {"bytes 1-9999999999999999999/3"},
            {"bytes 9999999999999999999-3/3"},
            {"bytes 1-2/9999999999999999999"}
        };
}
