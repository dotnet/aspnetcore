// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Net.Http.Headers
{
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

                var now = DateTimeOffset.Now;

                foreach (var quoted in new[] { true, false })
                {
                    for (var i = 0; i < 60; i++)
                    {
                        data.Add(now.AddSeconds(i), quoted);
                        data.Add(now.AddMinutes(i), quoted);
                        data.Add(now.AddDays(i), quoted);
                        data.Add(now.AddMonths(i), quoted);
                        data.Add(now.AddYears(i), quoted);
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
        public void TryParseSeconds_Fails(string headerValues, string targetValue)
        {
            TimeSpan? value;
            Assert.False(HeaderUtilities.TryParseSeconds(new StringValues(headerValues), targetValue, out value));
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
        public void ContainsCacheDirective_MatchesExactValue(string headerValues, string targetValue, bool contains)
        {
            Assert.Equal(contains, HeaderUtilities.ContainsCacheDirective(new StringValues(headerValues), targetValue));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("-1")]
        [InlineData("a")]
        [InlineData("1.1")]
        [InlineData("9223372036854775808")] // long.MaxValue + 1
        public void TryParseNonNegativeInt64_Fails(string valueString)
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
        public void TryParseNonNegativeInt32_Fails(string valueString)
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
    }
}
