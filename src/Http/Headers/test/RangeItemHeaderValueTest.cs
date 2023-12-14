// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class RangeItemHeaderValueTest
{
    [Fact]
    public void Ctor_BothValuesNull_Throw()
    {
        Assert.Throws<ArgumentException>(() => new RangeItemHeaderValue(null, null));
    }

    [Fact]
    public void Ctor_FromValueNegative_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeItemHeaderValue(-1, null));
    }

    [Fact]
    public void Ctor_FromGreaterThanToValue_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeItemHeaderValue(2, 1));
    }

    [Fact]
    public void Ctor_ToValueNegative_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RangeItemHeaderValue(null, -1));
    }

    [Fact]
    public void Ctor_ValidFormat_SuccessfullyCreated()
    {
        var rangeItem = new RangeItemHeaderValue(1, 2);
        Assert.Equal(1, rangeItem.From);
        Assert.Equal(2, rangeItem.To);
    }

    [Fact]
    public void ToString_UseDifferentRangeItems_AllSerializedCorrectly()
    {
        // Make sure ToString() doesn't add any separators.
        var rangeItem = new RangeItemHeaderValue(1000000000, 2000000000);
        Assert.Equal("1000000000-2000000000", rangeItem.ToString());

        rangeItem = new RangeItemHeaderValue(5, null);
        Assert.Equal("5-", rangeItem.ToString());

        rangeItem = new RangeItemHeaderValue(null, 10);
        Assert.Equal("-10", rangeItem.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentRangeItems_SameOrDifferentHashCodes()
    {
        var rangeItem1 = new RangeItemHeaderValue(1, 2);
        var rangeItem2 = new RangeItemHeaderValue(1, null);
        var rangeItem3 = new RangeItemHeaderValue(null, 2);
        var rangeItem4 = new RangeItemHeaderValue(2, 2);
        var rangeItem5 = new RangeItemHeaderValue(1, 2);

        Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem2.GetHashCode());
        Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem3.GetHashCode());
        Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem4.GetHashCode());
        Assert.Equal(rangeItem1.GetHashCode(), rangeItem5.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
    {
        var rangeItem1 = new RangeItemHeaderValue(1, 2);
        var rangeItem2 = new RangeItemHeaderValue(1, null);
        var rangeItem3 = new RangeItemHeaderValue(null, 2);
        var rangeItem4 = new RangeItemHeaderValue(2, 2);
        var rangeItem5 = new RangeItemHeaderValue(1, 2);

        Assert.False(rangeItem1.Equals(rangeItem2), "1-2 vs. 1-.");
        Assert.False(rangeItem2.Equals(rangeItem1), "1- vs. 1-2.");
        Assert.False(rangeItem1.Equals(null), "1-2 vs. null.");
        Assert.False(rangeItem1!.Equals(rangeItem3), "1-2 vs. -2.");
        Assert.False(rangeItem3.Equals(rangeItem1), "-2 vs. 1-2.");
        Assert.False(rangeItem1.Equals(rangeItem4), "1-2 vs. 2-2.");
        Assert.True(rangeItem1.Equals(rangeItem5), "1-2 vs. 1-2.");
    }

    [Fact]
    public void TryParse_DifferentValidScenarios_AllReturnNonZero()
    {
        CheckValidTryParse("1-2", 1, 2);
        CheckValidTryParse(" 1-2", 1, 2);
        CheckValidTryParse("0-0", 0, 0);
        CheckValidTryParse(" 1-", 1, null);
        CheckValidTryParse(" -2", null, 2);

        CheckValidTryParse(" 684684 - 123456789012345 ", 684684, 123456789012345);

        // The separator doesn't matter. It only parses until the first non-whitespace
        CheckValidTryParse(" 1 - 2 ,", 1, 2);

        CheckValidTryParse(",,1-2, 3 -  , , -6 , ,,", new Tuple<long?, long?>(1, 2), new Tuple<long?, long?>(3, null),
            new Tuple<long?, long?>(null, 6));
        CheckValidTryParse("1-2,", new Tuple<long?, long?>(1, 2));
        CheckValidTryParse("1-", new Tuple<long?, long?>(1, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(",,")]
    [InlineData("1")]
    [InlineData("1-2,3")]
    [InlineData("1--2")]
    [InlineData("1,-2")]
    [InlineData("-")]
    [InlineData("--")]
    [InlineData("2-1")]
    [InlineData("12345678901234567890123-")] // >>Int64.MaxValue
    [InlineData("-12345678901234567890123")] // >>Int64.MaxValue
    [InlineData("9999999999999999999-")] // 19-digit numbers outside the Int64 range.
    [InlineData("-9999999999999999999")] // 19-digit numbers outside the Int64 range.
    public void TryParse_DifferentInvalidScenarios_AllReturnFalse(string input)
    {
        RangeHeaderValue? result;
        Assert.False(RangeHeaderValue.TryParse("byte=" + input, out result));
    }

    private static void CheckValidTryParse(string input, long? expectedFrom, long? expectedTo)
    {
        RangeHeaderValue? result;
        Assert.True(RangeHeaderValue.TryParse("byte=" + input, out result), input);

        var ranges = result.Ranges.ToArray();
        Assert.Single(ranges);

        var range = ranges.First();

        Assert.Equal(expectedFrom, range.From);
        Assert.Equal(expectedTo, range.To);
    }

    private static void CheckValidTryParse(string input, params Tuple<long?, long?>[] expectedRanges)
    {
        RangeHeaderValue? result;
        Assert.True(RangeHeaderValue.TryParse("byte=" + input, out result), input);

        var ranges = result.Ranges.ToArray();
        Assert.Equal(expectedRanges.Length, ranges.Length);

        for (int i = 0; i < expectedRanges.Length; i++)
        {
            Assert.Equal(expectedRanges[i].Item1, ranges[i].From);
            Assert.Equal(expectedRanges[i].Item2, ranges[i].To);
        }
    }
}
