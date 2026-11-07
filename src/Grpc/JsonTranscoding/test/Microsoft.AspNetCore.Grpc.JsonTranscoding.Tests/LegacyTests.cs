// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Grpc.Shared;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class LegacyTests
{
    private const string TimestampPattern = @"^(?<datetime>[0-9]{4}-[01][0-9]-[0-3][0-9]T[012][0-9]:[0-5][0-9]:[0-5][0-9])(?<subseconds>\.[0-9]{1,9})?(?<offset>(Z|[+-][0-1][0-9]:[0-5][0-9]))$";
    private const string DurationPattern = @"^(?<sign>-)?(?<int>[0-9]{1,12})(?<subseconds>\.[0-9]{1,9})?s$";

    private static readonly Regex CompiledTimestampRegex = new Regex(TimestampPattern, RegexOptions.Compiled);
    private static readonly Regex CompiledDurationRegex = new Regex(DurationPattern, RegexOptions.Compiled);

    public static TheoryData<string> TimestampRegexInputs => new()
    {
        "",
        "1970-01-01T00:00:00Z",
        "1970-01-01T00:00:00Z\n",
        "2020-12-01T00:30:00.123456789+18:00",
        "0001-01-01T00:00:00-00:00",
        "9999-19-39T29:59:59.1-19:59",
        "2020-12-01t00:30:00z",
        "2020-12-01T00:30:00.1234567890Z",
        "2020-12-01T00:30:00",
        "٢٠٢٠-١٢-٠١T٠٠:٣٠:٠٠Z",
        new string('9', 100_000)
    };

    public static TheoryData<string> DurationRegexInputs => new()
    {
        "",
        "0s",
        "-0.000000001s",
        "123456789012.123456789s",
        "1s\n",
        "00s",
        "+1s",
        "1.1234567890s",
        "1S",
        "١s",
        new string('9', 100_000) + "s"
    };

    public static TheoryData<string, long, int> ValidTimestampValues => new()
    {
        { "1970-01-01T00:00:00Z", 0, 0 },
        { "1970-01-01T00:00:00Z\n", 0, 0 },
        { "2020-12-01T00:30:00.123456789+18:00", new DateTimeOffset(2020, 11, 30, 6, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(), 123456789 },
        { "2020-12-01T00:30:00.000000001-18:00", new DateTimeOffset(2020, 12, 1, 18, 30, 0, TimeSpan.Zero).ToUnixTimeSeconds(), 1 }
    };

    public static TheoryData<string> InvalidTimestampValues => new()
    {
        "2020-12-01t00:30:00z",
        "2020-12-01T00:30:00.1234567890Z",
        "2020-12-01T00:30:00-00:00",
        "2020-02-30T00:30:00Z",
        "9999-12-31T23:59:59-18:00",
        new string('9', 100_000)
    };

    public static TheoryData<string, long, int> ValidDurationValues => new()
    {
        { "0s", 0, 0 },
        { "-0s", 0, 0 },
        { "1s\n", 1, 0 },
        { "1234567890.123456789s", 1234567890, 123456789 }
    };

    public static TheoryData<string> InvalidDurationValues => new()
    {
        "00s",
        "+1s",
        "-0.000000001s",
        "253402300800s",
        "1.1234567890s",
        new string('9', 100_000) + "s"
    };

    [Theory]
    [MemberData(nameof(TimestampRegexInputs))]
    public void TimestampRegex_GeneratedRegexMatchesCompiledRegex(string value)
    {
        AssertRegexEquivalent(CompiledTimestampRegex, Legacy.TimestampRegex(), value);
    }

    [Theory]
    [MemberData(nameof(DurationRegexInputs))]
    public void DurationRegex_GeneratedRegexMatchesCompiledRegex(string value)
    {
        AssertRegexEquivalent(CompiledDurationRegex, Legacy.DurationRegex(), value);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void GeneratedRegexes_MatchCompiledRegexAcrossCultures(string cultureName)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            AssertRegexEquivalent(new Regex(TimestampPattern, RegexOptions.Compiled), Legacy.TimestampRegex(), "2020-12-01T00:30:00.123456789+18:00");
            AssertRegexEquivalent(new Regex(DurationPattern, RegexOptions.Compiled), Legacy.DurationRegex(), "-123456789012.123456789s");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [Theory]
    [MemberData(nameof(ValidTimestampValues))]
    public void ParseTimestamp_ValidValuePreservesOutput(string value, long expectedSeconds, int expectedNanos)
    {
        Assert.Equal((expectedSeconds, expectedNanos), Legacy.ParseTimestamp(value));
    }

    [Theory]
    [MemberData(nameof(InvalidTimestampValues))]
    public void ParseTimestamp_InvalidValuePreservesError(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Legacy.ParseTimestamp(value));

        Assert.Equal($"Invalid Timestamp value: {value}", exception.Message);
    }

    [Theory]
    [MemberData(nameof(ValidDurationValues))]
    public void ParseDuration_ValidValuePreservesOutput(string value, long expectedSeconds, int expectedNanos)
    {
        Assert.Equal((expectedSeconds, expectedNanos), Legacy.ParseDuration(value));
    }

    [Theory]
    [MemberData(nameof(InvalidDurationValues))]
    public void ParseDuration_InvalidValuePreservesError(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Legacy.ParseDuration(value));

        Assert.Equal($"Invalid Duration value: {value}", exception.Message);
    }

    private static void AssertRegexEquivalent(Regex expectedRegex, Regex actualRegex, string value)
    {
        Assert.Equal(expectedRegex.Options, actualRegex.Options);
        Assert.Equal(expectedRegex.MatchTimeout, actualRegex.MatchTimeout);
        Assert.Equal(expectedRegex.RightToLeft, actualRegex.RightToLeft);
        Assert.Equal(expectedRegex.GetGroupNames(), actualRegex.GetGroupNames());
        Assert.Equal(expectedRegex.GetGroupNumbers(), actualRegex.GetGroupNumbers());

        var expectedMatch = expectedRegex.Match(value);
        var actualMatch = actualRegex.Match(value);
        Assert.Equal(expectedMatch.Success, actualMatch.Success);
        Assert.Equal(expectedMatch.Index, actualMatch.Index);
        Assert.Equal(expectedMatch.Length, actualMatch.Length);
        Assert.Equal(expectedMatch.Value, actualMatch.Value);

        foreach (var groupName in expectedRegex.GetGroupNames())
        {
            var expectedGroup = expectedMatch.Groups[groupName];
            var actualGroup = actualMatch.Groups[groupName];
            Assert.Equal(expectedGroup.Success, actualGroup.Success);
            Assert.Equal(expectedGroup.Index, actualGroup.Index);
            Assert.Equal(expectedGroup.Length, actualGroup.Length);
            Assert.Equal(expectedGroup.Value, actualGroup.Value);
            Assert.Equal(expectedGroup.Captures.Count, actualGroup.Captures.Count);

            for (var i = 0; i < expectedGroup.Captures.Count; i++)
            {
                Assert.Equal(expectedGroup.Captures[i].Index, actualGroup.Captures[i].Index);
                Assert.Equal(expectedGroup.Captures[i].Length, actualGroup.Captures[i].Length);
                Assert.Equal(expectedGroup.Captures[i].Value, actualGroup.Captures[i].Value);
            }
        }
    }
}
