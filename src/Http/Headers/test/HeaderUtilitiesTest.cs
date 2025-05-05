// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers;

public class HeaderUtilitiesTest
{
    private const string Rfc1123Format = "r";

    [Theory]
    [MemberData(nameof(TestValues))]
    public void ReturnsSameResultAsRfc1123String(DateTimeOffset dateTime, bool quoted)
    {
        var formatted = dateTime.ToString(Rfc1123Format);
        var expected = quoted ? $"\"{formatted}\"" : formatted;
        var actual = HeaderUtilities.FormatDate(dateTime, quoted);

        Assert.Equal(expected, actual);
    }

    public static TheoryData<DateTimeOffset, bool> TestValues
    {
        get
        {
            var data = new TheoryData<DateTimeOffset, bool>();

            var date = new DateTimeOffset(new DateTime(2018, 1, 1, 1, 1, 1));

            foreach (var quoted in new[] { true, false })
            {
                data.Add(date, quoted);

                for (var i = 1; i < 60; i++)
                {
                    data.Add(date.AddSeconds(i), quoted);
                    data.Add(date.AddMinutes(i), quoted);
                }

                for (var i = 1; i < DateTime.DaysInMonth(date.Year, date.Month); i++)
                {
                    data.Add(date.AddDays(i), quoted);
                }

                for (var i = 1; i < 11; i++)
                {
                    data.Add(date.AddMonths(i), quoted);
                }

                for (var i = 1; i < 5; i++)
                {
                    data.Add(date.AddYears(i), quoted);
                }
            }

            return data;
        }
    }

    [Theory]
    [InlineData("h=1", "h", 1)]
    [InlineData("directive1=3, directive2=10", "directive1", 3)]
    [InlineData("directive1   =45, directive2=80", "directive1", 45)]
    [InlineData("directive1=   89   , directive2=22", "directive1", 89)]
    [InlineData("directive1=   89   , directive2= 42", "directive2", 42)]
    [InlineData("directive1=   89   , directive= 42", "directive", 42)]
    [InlineData("directive1,,,,,directive2 = 42 ", "directive2", 42)]
    [InlineData("directive1=;,directive2 = 42 ", "directive2", 42)]
    [InlineData("directive1;;,;;,directive2 = 42 ", "directive2", 42)]
    [InlineData("directive1=value;q=0.6,directive2 = 42 ", "directive2", 42)]
    public void TryParseSeconds_Succeeds(string headerValues, string targetValue, int expectedValue)
    {
        TimeSpan? value;
        Assert.True(HeaderUtilities.TryParseSeconds(new StringValues(headerValues), targetValue, out value));
        Assert.Equal(TimeSpan.FromSeconds(expectedValue), value);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("h=", "h")]
    [InlineData("directive1=, directive2=10", "directive1")]
    [InlineData("directive1   , directive2=80", "directive1")]
    [InlineData("h=10", "directive")]
    [InlineData("directive1", "directive")]
    [InlineData("directive1,,,,,,,", "directive")]
    [InlineData("h=directive", "directive")]
    [InlineData("directive1, directive2=80", "directive")]
    [InlineData("directive1=;, directive2=10", "directive1")]
    [InlineData("directive1;directive2=10", "directive2")]
    public void TryParseSeconds_Fails(string? headerValues, string? targetValue)
    {
        TimeSpan? value;
        Assert.False(HeaderUtilities.TryParseSeconds(new StringValues(headerValues), targetValue!, out value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1234567890)]
    [InlineData(long.MaxValue)]
    public void FormatNonNegativeInt64_MatchesToString(long value)
    {
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), HeaderUtilities.FormatNonNegativeInt64(value));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1234567890)]
    [InlineData(long.MinValue)]
    public void FormatNonNegativeInt64_Throws_ForNegativeValues(long value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HeaderUtilities.FormatNonNegativeInt64(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(-1234567890)]
    [InlineData(1234567890)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void FormatInt64_MatchesToString(long value)
    {
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), HeaderUtilities.FormatInt64(value));
    }

    [Theory]
    [InlineData("h", "h", true)]
    [InlineData("h=", "h", true)]
    [InlineData("h=1", "h", true)]
    [InlineData("H", "h", true)]
    [InlineData("H=", "h", true)]
    [InlineData("H=1", "h", true)]
    [InlineData("h", "H", true)]
    [InlineData("h=", "H", true)]
    [InlineData("h=1", "H", true)]
    [InlineData("directive1, directive=10", "directive1", true)]
    [InlineData("directive1=, directive=10", "directive1", true)]
    [InlineData("directive1=3, directive=10", "directive1", true)]
    [InlineData("directive1   , directive=80", "directive1", true)]
    [InlineData("   directive1, directive=80", "directive1", true)]
    [InlineData("directive1   =45, directive=80", "directive1", true)]
    [InlineData("directive1=   89   , directive=22", "directive1", true)]
    [InlineData("directive1, directive", "directive", true)]
    [InlineData("directive1, directive=", "directive", true)]
    [InlineData("directive1, directive=10", "directive", true)]
    [InlineData("directive1=3, directive", "directive", true)]
    [InlineData("directive1=3, directive=", "directive", true)]
    [InlineData("directive1=3, directive=10", "directive", true)]
    [InlineData("directive1=   89   , directive= 42", "directive", true)]
    [InlineData("directive1=   89   , directive = 42", "directive", true)]
    [InlineData("directive1,,,,,directive2 = 42 ", "directive2", true)]
    [InlineData("directive1;;,;;,directive2 = 42 ", "directive2", true)]
    [InlineData("directive1=;,directive2 = 42 ", "directive2", true)]
    [InlineData("directive1=value;q=0.6,directive2 = 42 ", "directive2", true)]
    [InlineData(null, null, false)]
    [InlineData(null, "", false)]
    [InlineData("", null, false)]
    [InlineData("", "", false)]
    [InlineData("h=10", "directive", false)]
    [InlineData("directive1", "directive", false)]
    [InlineData("directive1,,,,,,,", "directive", false)]
    [InlineData("h=directive", "directive", false)]
    [InlineData("directive1, directive2=80", "directive", false)]
    [InlineData("directive1;, directive2=80", "directive", false)]
    [InlineData("directive1=value;q=0.6;directive2 = 42 ", "directive2", false)]
    public void ContainsCacheDirective_MatchesExactValue(string? headerValues, string? targetValue, bool contains)
    {
        Assert.Equal(contains, HeaderUtilities.ContainsCacheDirective(new StringValues(headerValues), targetValue!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("-1")]
    [InlineData("a")]
    [InlineData("1.1")]
    [InlineData("9223372036854775808")] // long.MaxValue + 1
    public void TryParseNonNegativeInt64_Fails(string? valueString)
    {
        long value = 1;
        Assert.False(HeaderUtilities.TryParseNonNegativeInt64(valueString, out value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("9223372036854775807", 9223372036854775807)] // long.MaxValue
    public void TryParseNonNegativeInt64_Succeeds(string valueString, long expected)
    {
        long value = 1;
        Assert.True(HeaderUtilities.TryParseNonNegativeInt64(valueString, out value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("-1")]
    [InlineData("a")]
    [InlineData("1.1")]
    [InlineData("1,000")]
    [InlineData("2147483648")] // int.MaxValue + 1
    public void TryParseNonNegativeInt32_Fails(string? valueString)
    {
        int value = 1;
        Assert.False(HeaderUtilities.TryParseNonNegativeInt32(valueString, out value));
        Assert.Equal(0, value);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("2147483647", 2147483647)] // int.MaxValue
    public void TryParseNonNegativeInt32_Succeeds(string valueString, long expected)
    {
        int value = 1;
        Assert.True(HeaderUtilities.TryParseNonNegativeInt32(valueString, out value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("\"hello", "\"hello")]
    [InlineData("hello\"", "hello\"")]
    [InlineData("\"\"hello\"\"", "\"hello\"")]
    public void RemoveQuotes_BehaviorCheck(string input, string expected)
    {
        var actual = HeaderUtilities.RemoveQuotes(input);

        Assert.Equal(expected.AsSpan(), actual);
    }
    [Theory]
    [InlineData("\"hello\"", true)]
    [InlineData("\"hello", false)]
    [InlineData("hello\"", false)]
    [InlineData("\"\"hello\"\"", true)]
    public void IsQuoted_BehaviorCheck(string input, bool expected)
    {
        var actual = HeaderUtilities.IsQuoted(input);

        Assert.Equal(expected, actual);
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
    public void UnescapeAsQuotedString_BehaviorCheck(string input, string expected)
    {
        var actual = HeaderUtilities.UnescapeAsQuotedString(input);

        Assert.Equal(expected.AsSpan(), actual);
    }

    [Theory]
    [InlineData("value", "\"value\"")]
    [InlineData("23", "\"23\"")]
    [InlineData(";;;", "\";;;\"")]
    [InlineData("\"value\"", "\"\\\"value\\\"\"")]
    [InlineData("unquoted \"value", "\"unquoted \\\"value\"")]
    [InlineData("value\\morevalues\\evenmorevalues", "\"value\\\\morevalues\\\\evenmorevalues\"")]
    // We have to assume that the input needs to be quoted here
    [InlineData("\"\"double quoted string\"\"", "\"\\\"\\\"double quoted string\\\"\\\"\"")]
    [InlineData("\t", "\"\t\"")]
    public void SetAndEscapeValue_BehaviorCheck(string input, string expected)
    {
        var actual = HeaderUtilities.EscapeAsQuotedString(input);

        Assert.Equal(expected.AsSpan(), actual);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\b")]
    [InlineData("\r")]
    public void SetAndEscapeValue_ControlCharactersThrowFormatException(string input)
    {
        Assert.Throws<FormatException>(() => { var actual = HeaderUtilities.EscapeAsQuotedString(input); });
    }

    [Fact]
    public void SetAndEscapeValue_ThrowsFormatExceptionOnDelCharacter()
    {
        Assert.Throws<FormatException>(() => { var actual = HeaderUtilities.EscapeAsQuotedString($"{(char)0x7F}"); });
    }

    [Theory]
    [InlineData("text;q=0", 0d, 1)]
    [InlineData("text;q=1", 1d, 1)]
    public void TryParseQualityDouble_WithoutDecimalPart_ReturnsCorrectQuality(
        string inputString,
        double expectedQuality,
        int expectedLength)
        => VerifyTryParseQualityDoubleSuccess(inputString, 7, expectedQuality, expectedLength);

    [Theory]
    [InlineData("text;q=0,*;q=1", 0d, 1)]
    [InlineData("text;q=1,*;q=0", 1d, 1)]
    public void TryParseQualityDouble_WithoutDecimalPart_WithSubsequentCharacters_ReturnsCorrectQuality(
        string inputString,
        double expectedQuality,
        int expectedLength)
        => VerifyTryParseQualityDoubleSuccess(inputString, 7, expectedQuality, expectedLength);

    [Theory]
    [InlineData("text;q=0.", 0d, 2)]
    [InlineData("text;q=0.0", 0d, 3)]
    [InlineData("text;q=0.00000000", 0d, 10)]
    [InlineData("text;q=1.", 1d, 2)]
    [InlineData("text;q=1.0", 1d, 3)]
    [InlineData("text;q=1.000", 1d, 5)]
    [InlineData("text;q=1.00000000", 1d, 10)]
    [InlineData("text;q=0.1", 0.1d, 3)]
    [InlineData("text;q=0.001", 0.001d, 5)]
    [InlineData("text;q=0.00000001", 0.00000001d, 10)]
    [InlineData("text;q=0.12345678", 0.12345678d, 10)]
    [InlineData("text;q=0.98765432", 0.98765432d, 10)]
    public void TryParseQualityDouble_WithDecimalPart_ReturnsCorrectQuality(
        string inputString, double expectedQuality, int expectedLength)
        => VerifyTryParseQualityDoubleSuccess(inputString, 7, expectedQuality, expectedLength);

    [Theory]
    [InlineData("text;q=0.,*;q=1", 0d, 2)]
    [InlineData("text;q=0.0,*;q=1", 0d, 3)]
    [InlineData("text;q=0.00000000,*;q=1", 0d, 10)]
    [InlineData("text;q=1.,*;q=1", 1d, 2)]
    [InlineData("text;q=1.0,*;q=1", 1d, 3)]
    [InlineData("text;q=1.000,*;q=1", 1d, 5)]
    [InlineData("text;q=1.00000000,*;q=1", 1d, 10)]
    [InlineData("text;q=0.1,*;q=1", 0.1d, 3)]
    [InlineData("text;q=0.001,*;q=1", 0.001d, 5)]
    [InlineData("text;q=0.00000001,*;q=1", 0.00000001d, 10)]
    [InlineData("text;q=0.12345678,*;q=1", 0.12345678d, 10)]
    [InlineData("text;q=0.98765432,*;q=1", 0.98765432d, 10)]
    public void TryParseQualityDouble_WithDecimalPart_WithSubsequentCharacters_ReturnsCorrectQuality(
        string inputString, double expectedQuality, int expectedLength)
        => VerifyTryParseQualityDoubleSuccess(inputString, 7, expectedQuality, expectedLength);

    private static void VerifyTryParseQualityDoubleSuccess(string inputString, int startIndex, double expectedQuality, int expectedLength)
    {
        // Arrange
        var input = new StringSegment(inputString);

        // Act
        var result = HeaderUtilities.TryParseQualityDouble(input, startIndex, out var actualQuality, out var actualLength);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedQuality, actualQuality);
        Assert.Equal(expectedLength, actualLength);
    }

    [Fact]
    public void TryParseQualityDouble_StartIndexIsOutOfRange_ReturnsFalse()
        => VerifyTryParseQualityDoubleFailure("text;q=0.1", 10);

    [Theory]
    [InlineData("text;q=2")]
    [InlineData("text;q=a")]
    [InlineData("text;q=.1")]
    [InlineData("text;q=/.1")]
    [InlineData("text;q=:.1")]
    public void TryParseQualityDouble_HasInvalidStartingCharacter_ReturnsFalse(string inputString)
        => VerifyTryParseQualityDoubleFailure(inputString, 7);

    [Theory]
    [InlineData("text;q=00")]
    [InlineData("text;q=00.")]
    [InlineData("text;q=00.0")]
    [InlineData("text;q=01.0")]
    [InlineData("text;q=10")]
    [InlineData("text;q=10.")]
    [InlineData("text;q=10.0")]
    [InlineData("text;q=11.0")]
    public void TryParseQualityDouble_HasMoreThanOneDigitBeforeDot_ReturnsFalse(string inputString)
        => VerifyTryParseQualityDoubleFailure(inputString, 7);

    [Theory]
    [InlineData("text;q=0.000000000")]
    [InlineData("text;q=1.000000000")]
    [InlineData("text;q=0.000000001")]
    public void TryParseQualityDouble_ExceedsQualityValueMaxCharacterCount_ReturnsFalse(string inputString)
        => VerifyTryParseQualityDoubleFailure(inputString, 7);

    [Theory]
    [InlineData("text;q=1.000000001")]
    [InlineData("text;q=2")]
    public void TryParseQualityDouble_ParsedQualityIsGreaterThanOne_ReturnsFalse(string inputString)
        => VerifyTryParseQualityDoubleFailure(inputString, 7);

    private static void VerifyTryParseQualityDoubleFailure(string inputString, int startIndex)
    {
        // Arrange
        var input = new StringSegment(inputString);

        // Act
        var result = HeaderUtilities.TryParseQualityDouble(input, startIndex, out var quality, out var length);

        // Assert
        Assert.False(result);
        Assert.Equal(0, quality);
        Assert.Equal(0, length);
    }
}
