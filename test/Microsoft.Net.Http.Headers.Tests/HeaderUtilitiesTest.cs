// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Net.Http.Headers
{
    public static class HeaderUtilitiesTest
    {
        private const string Rfc1123Format = "r";

        [Theory]
        [MemberData(nameof(TestValues))]
        public static void ReturnsSameResultAsRfc1123String(DateTimeOffset dateTime, bool quoted)
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
    }
}
