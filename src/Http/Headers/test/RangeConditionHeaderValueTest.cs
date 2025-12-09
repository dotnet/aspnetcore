// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class RangeConditionHeaderValueTest
{
    [Fact]
    public void Ctor_EntityTagOverload_MatchExpectation()
    {
        var rangeCondition = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
        Assert.Equal(new EntityTagHeaderValue("\"x\""), rangeCondition.EntityTag);
        Assert.Null(rangeCondition.LastModified);

        EntityTagHeaderValue input = null!;
        Assert.Throws<ArgumentNullException>(() => new RangeConditionHeaderValue(input));
    }

    [Fact]
    public void Ctor_EntityTagStringOverload_MatchExpectation()
    {
        var rangeCondition = new RangeConditionHeaderValue("\"y\"");
        Assert.Equal(new EntityTagHeaderValue("\"y\""), rangeCondition.EntityTag);
        Assert.Null(rangeCondition.LastModified);

        Assert.Throws<ArgumentException>(() => new RangeConditionHeaderValue((string?)null));
    }

    [Fact]
    public void Ctor_DateOverload_MatchExpectation()
    {
        var rangeCondition = new RangeConditionHeaderValue(
            new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        Assert.Null(rangeCondition.EntityTag);
        Assert.Equal(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero), rangeCondition.LastModified);
    }

    [Fact]
    public void ToString_UseDifferentRangeConditions_AllSerializedCorrectly()
    {
        var rangeCondition = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
        Assert.Equal("\"x\"", rangeCondition.ToString());

        rangeCondition = new RangeConditionHeaderValue(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        Assert.Equal("Thu, 15 Jul 2010 12:33:57 GMT", rangeCondition.ToString());
    }

    [Fact]
    public void GetHashCode_UseSameAndDifferentRangeConditions_SameOrDifferentHashCodes()
    {
        var rangeCondition1 = new RangeConditionHeaderValue("\"x\"");
        var rangeCondition2 = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
        var rangeCondition3 = new RangeConditionHeaderValue(
            new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        var rangeCondition4 = new RangeConditionHeaderValue(
            new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
        var rangeCondition5 = new RangeConditionHeaderValue(
            new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        var rangeCondition6 = new RangeConditionHeaderValue(
            new EntityTagHeaderValue("\"x\"", true));

        Assert.Equal(rangeCondition1.GetHashCode(), rangeCondition2.GetHashCode());
        Assert.NotEqual(rangeCondition1.GetHashCode(), rangeCondition3.GetHashCode());
        Assert.NotEqual(rangeCondition3.GetHashCode(), rangeCondition4.GetHashCode());
        Assert.Equal(rangeCondition3.GetHashCode(), rangeCondition5.GetHashCode());
        Assert.NotEqual(rangeCondition1.GetHashCode(), rangeCondition6.GetHashCode());
    }

    [Fact]
    public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
    {
        var rangeCondition1 = new RangeConditionHeaderValue("\"x\"");
        var rangeCondition2 = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
        var rangeCondition3 = new RangeConditionHeaderValue(
            new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        var rangeCondition4 = new RangeConditionHeaderValue(
            new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
        var rangeCondition5 = new RangeConditionHeaderValue(
            new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
        var rangeCondition6 = new RangeConditionHeaderValue(
            new EntityTagHeaderValue("\"x\"", true));

        Assert.False(rangeCondition1.Equals(null), "\"x\" vs. <null>");
        Assert.True(rangeCondition1!.Equals(rangeCondition2), "\"x\" vs. \"x\"");
        Assert.False(rangeCondition1.Equals(rangeCondition3), "\"x\" vs. date");
        Assert.False(rangeCondition3.Equals(rangeCondition1), "date vs. \"x\"");
        Assert.False(rangeCondition3.Equals(rangeCondition4), "date vs. different date");
        Assert.True(rangeCondition3.Equals(rangeCondition5), "date vs. date");
        Assert.False(rangeCondition1.Equals(rangeCondition6), "\"x\" vs. W/\"x\"");
    }

    [Fact]
    public void Parse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidParse("  \"x\" ", new RangeConditionHeaderValue("\"x\""));
        CheckValidParse("  Sun, 06 Nov 1994 08:49:37 GMT ",
            new RangeConditionHeaderValue(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
        CheckValidParse("Wed, 09 Nov 1994 08:49:37 GMT",
            new RangeConditionHeaderValue(new DateTimeOffset(1994, 11, 9, 8, 49, 37, TimeSpan.Zero)));
        CheckValidParse(" W/ \"tag\" ", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"", true)));
        CheckValidParse(" w/\"tag\"", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"", true)));
        CheckValidParse("\"tag\"", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"")));
    }

    [Theory]
    [InlineData("\"x\" ,")] // no delimiter allowed
    [InlineData("Sun, 06 Nov 1994 08:49:37 GMT ,")] // no delimiter allowed
    [InlineData("\"x\" Sun, 06 Nov 1994 08:49:37 GMT")]
    [InlineData("Sun, 06 Nov 1994 08:49:37 GMT \"x\"")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Wed 09 Nov 1994 08:49:37 GMT")]
    [InlineData("\"x")]
    [InlineData("Wed, 09 Nov")]
    [InlineData("W/Wed 09 Nov 1994 08:49:37 GMT")]
    [InlineData("\"x\",")]
    [InlineData("Wed 09 Nov 1994 08:49:37 GMT,")]
    public void Parse_SetOfInvalidValueStrings_Throws(string? input)
    {
        Assert.Throws<FormatException>(() => RangeConditionHeaderValue.Parse(input));
    }

    [Fact]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
    {
        CheckValidTryParse("  \"x\" ", new RangeConditionHeaderValue("\"x\""));
        CheckValidTryParse("  Sun, 06 Nov 1994 08:49:37 GMT ",
            new RangeConditionHeaderValue(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
        CheckValidTryParse(" W/ \"tag\" ", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"", true)));
        CheckValidTryParse(" w/\"tag\"", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"", true)));
        CheckValidTryParse("\"tag\"", new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"")));
    }

    [Theory]
    [InlineData("\"x\" ,")] // no delimiter allowed
    [InlineData("Sun, 06 Nov 1994 08:49:37 GMT ,")] // no delimiter allowed
    [InlineData("\"x\" Sun, 06 Nov 1994 08:49:37 GMT")]
    [InlineData("Sun, 06 Nov 1994 08:49:37 GMT \"x\"")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Wed 09 Nov 1994 08:49:37 GMT")]
    [InlineData("\"x")]
    [InlineData("Wed, 09 Nov")]
    [InlineData("W/Wed 09 Nov 1994 08:49:37 GMT")]
    [InlineData("\"x\",")]
    [InlineData("Wed 09 Nov 1994 08:49:37 GMT,")]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse(string? input)
    {
        Assert.False(RangeConditionHeaderValue.TryParse(input, out var result));
        Assert.Null(result);
    }

    #region Helper methods

    private void CheckValidParse(string input, RangeConditionHeaderValue expectedResult)
    {
        var result = RangeConditionHeaderValue.Parse(input);
        Assert.Equal(expectedResult, result);
    }

    private void CheckValidTryParse(string input, RangeConditionHeaderValue expectedResult)
    {
        Assert.True(RangeConditionHeaderValue.TryParse(input, out var result));
        Assert.Equal(expectedResult, result);
    }

    #endregion
}
