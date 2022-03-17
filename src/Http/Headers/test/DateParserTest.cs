// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Net.Http.Headers;

public class DateParserTest
{
    [Theory]
    [MemberData(nameof(ValidStringData))]
    public void TryParse_SetOfValidValueStrings_ParsedCorrectly(string input, DateTimeOffset expected)
    {
        // We don't need to validate all possible date values, since they're already tested in HttpRuleParserTest.
        // Just make sure the parser calls HttpRuleParser methods correctly.
        Assert.True(HeaderUtilities.TryParseDate(input, out var result));
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> ValidStringData()
    {
        yield return new object[] { "Tue, 15 Nov 1994 08:12:31 GMT", new DateTimeOffset(1994, 11, 15, 8, 12, 31, TimeSpan.Zero) };
        yield return new object[] { "      Sunday, 06-Nov-94 08:49:37 GMT   ", new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero) };
        yield return new object[] { " Tue,\r\n 15 Nov\r\n 1994 08:12:31 GMT   ", new DateTimeOffset(1994, 11, 15, 8, 12, 31, TimeSpan.Zero) };
        yield return new object[] { "Sat, 09-Dec-2017 07:07:03 GMT ", new DateTimeOffset(2017, 12, 09, 7, 7, 3, TimeSpan.Zero) };
    }

    [Theory]
    [MemberData(nameof(InvalidStringData))]
    public void TryParse_SetOfInvalidValueStrings_ReturnsFalse(string input)
    {
        Assert.False(HeaderUtilities.TryParseDate(input, out var result));
        Assert.Equal(new DateTimeOffset(), result);
    }

    public static IEnumerable<object?[]> InvalidStringData()
    {
        yield return new object?[] { null };
        yield return new object[] { string.Empty };
        yield return new object[] { "  " };
        yield return new object[] { "!!Sunday, 06-Nov-94 08:49:37 GMT" };
    }

    [Fact]
    public void ToString_UseDifferentValues_MatchExpectation()
    {
        Assert.Equal("Sat, 31 Jul 2010 15:38:57 GMT",
            HeaderUtilities.FormatDate(new DateTimeOffset(2010, 7, 31, 15, 38, 57, TimeSpan.Zero)));

        Assert.Equal("Fri, 01 Jan 2010 01:01:01 GMT",
            HeaderUtilities.FormatDate(new DateTimeOffset(2010, 1, 1, 1, 1, 1, TimeSpan.Zero)));
    }
}
