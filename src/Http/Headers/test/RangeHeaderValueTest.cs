// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class RangeHeaderValueTest
{
    [Fact]
    public void Ctor_InvalidRange_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeHeaderValue(5, 2));
    }

    [Fact]
    public void Unit_GetAndSetValidAndInvalidValues_MatchExpectation()
    {
        var range = new RangeHeaderValue();
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
        var range = new RangeHeaderValue();
        range.Unit = "myunit";
        range.Ranges.Add(new RangeItemHeaderValue(1, 3));
        Assert.Equal("myunit=1-3", range.ToString());

        range.Ranges.Add(new RangeItemHeaderValue(5, null));
        range.Ranges.Add(new RangeItemHeaderValue(null, 17));
        Assert.Equal("myunit=1-3, 5-, -17", range.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
    {
        var range1 = new RangeHeaderValue(1, 2);
        var range2 = new RangeHeaderValue(1, 2);
        range2.Unit = "BYTES";
        var range3 = new RangeHeaderValue(1, null);
        var range4 = new RangeHeaderValue(null, 2);
        var range5 = new RangeHeaderValue();
        range5.Ranges.Add(new RangeItemHeaderValue(1, 2));
        range5.Ranges.Add(new RangeItemHeaderValue(3, 4));
        var range6 = new RangeHeaderValue();
        range6.Ranges.Add(new RangeItemHeaderValue(3, 4)); // reverse order of range5
        range6.Ranges.Add(new RangeItemHeaderValue(1, 2));

        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());
        Assert.NotEqual(range1.GetHashCode(), range3.GetHashCode());
        Assert.NotEqual(range1.GetHashCode(), range4.GetHashCode());
        Assert.NotEqual(range1.GetHashCode(), range5.GetHashCode());
        Assert.Equal(range5.GetHashCode(), range6.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
    {
        var range1 = new RangeHeaderValue(1, 2);
        var range2 = new RangeHeaderValue(1, 2);
        range2.Unit = "BYTES";
        var range3 = new RangeHeaderValue(1, null);
        var range4 = new RangeHeaderValue(null, 2);
        var range5 = new RangeHeaderValue();
        range5.Ranges.Add(new RangeItemHeaderValue(1, 2));
        range5.Ranges.Add(new RangeItemHeaderValue(3, 4));
        var range6 = new RangeHeaderValue();
        range6.Ranges.Add(new RangeItemHeaderValue(3, 4)); // reverse order of range5
        range6.Ranges.Add(new RangeItemHeaderValue(1, 2));
        var range7 = new RangeHeaderValue(1, 2);
        range7.Unit = "other";

        Assert.False(range1.Equals(null), "bytes=1-2 vs. <null>");
        Assert.True(range1!.Equals(range2), "bytes=1-2 vs. BYTES=1-2");
        Assert.False(range1.Equals(range3), "bytes=1-2 vs. bytes=1-");
        Assert.False(range1.Equals(range4), "bytes=1-2 vs. bytes=-2");
        Assert.False(range1.Equals(range5), "bytes=1-2 vs. bytes=1-2,3-4");
        Assert.True(range5.Equals(range6), "bytes=1-2,3-4 vs. bytes=3-4,1-2");
        Assert.False(range1.Equals(range7), "bytes=1-2 vs. other=1-2");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse(" bytes=1-2 ", new RangeHeaderValue(1, 2));

        var expected = new RangeHeaderValue();
        expected.Unit = "custom";
        expected.Ranges.Add(new RangeItemHeaderValue(null, 5));
        expected.Ranges.Add(new RangeItemHeaderValue(1, 4));
        CheckValidParse("custom = -  5 , 1 - 4 ,,", expected);

        expected = new RangeHeaderValue();
        expected.Unit = "custom";
        expected.Ranges.Add(new RangeItemHeaderValue(1, 2));
        CheckValidParse(" custom = 1 - 2", expected);

        expected = new RangeHeaderValue();
        expected.Ranges.Add(new RangeItemHeaderValue(1, 2));
        expected.Ranges.Add(new RangeItemHeaderValue(3, null));
        expected.Ranges.Add(new RangeItemHeaderValue(null, 4));
        CheckValidParse("bytes =1-2,,3-, , ,-4,,", expected);
    }

    [Fact]
    public void Parse_SetOfInvalidValueStrings_Throws()
    {
        CheckInvalidParse("bytes=1-2x"); // only delimiter ',' allowed after last range
        CheckInvalidParse("x bytes=1-2");
        CheckInvalidParse("bytes=1-2.4");
        CheckInvalidParse(null);
        CheckInvalidParse(string.Empty);

        CheckInvalidParse("bytes=1");
        CheckInvalidParse("bytes=");
        CheckInvalidParse("bytes");
        CheckInvalidParse("bytes 1-2");
        CheckInvalidParse("bytes= ,,, , ,,");
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse(" bytes=1-2 ", new RangeHeaderValue(1, 2));

        var expected = new RangeHeaderValue();
        expected.Unit = "custom";
        expected.Ranges.Add(new RangeItemHeaderValue(null, 5));
        expected.Ranges.Add(new RangeItemHeaderValue(1, 4));
        CheckValidTryParse("custom = -  5 , 1 - 4 ,,", expected);
    }

    [Fact]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
    {
        CheckInvalidTryParse("bytes=1-2x"); // only delimiter ',' allowed after last range
        CheckInvalidTryParse("x bytes=1-2");
        CheckInvalidTryParse("bytes=1-2.4");
        CheckInvalidTryParse(null);
        CheckInvalidTryParse(string.Empty);
    }

    #region Helper methods

    private void CheckValidParse(string? input, RangeHeaderValue expectedResult)
    {
        var result = RangeHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidParse(string? input)
    {
        Assert.Throws<FormatException>(() => RangeHeaderValue.Parse(input));
    }

    private void CheckValidTryParse(string? input, RangeHeaderValue expectedResult)
    {
        Assert.True(RangeHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    private void CheckInvalidTryParse(string? input)
    {
        Assert.False(RangeHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    #endregion
}
