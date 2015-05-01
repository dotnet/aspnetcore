// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Net.Http.Headers
{
    public class DateParserTest
    {
        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // We don't need to validate all possible date values, since they're already tested in HttpRuleParserTest.
            // Just make sure the parser calls HttpRuleParser methods correctly.
            CheckValidParsedValue("Tue, 15 Nov 1994 08:12:31 GMT", new DateTimeOffset(1994, 11, 15, 8, 12, 31, TimeSpan.Zero));
            CheckValidParsedValue("      Sunday, 06-Nov-94 08:49:37 GMT   ", new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero));
            CheckValidParsedValue(" Tue,\r\n 15 Nov\r\n 1994 08:12:31 GMT   ", new DateTimeOffset(1994, 11, 15, 8, 12, 31, TimeSpan.Zero));
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidParsedValue(null);
            CheckInvalidParsedValue(string.Empty);
            CheckInvalidParsedValue("  ");
            CheckInvalidParsedValue("!!Sunday, 06-Nov-94 08:49:37 GMT");
        }

        [Fact]
        public void ToString_UseDifferentValues_MatchExpectation()
        {
            Assert.Equal("Sat, 31 Jul 2010 15:38:57 GMT",
                HeaderUtilities.FormatDate(new DateTimeOffset(2010, 7, 31, 15, 38, 57, TimeSpan.Zero)));

            Assert.Equal("Fri, 01 Jan 2010 01:01:01 GMT",
                HeaderUtilities.FormatDate(new DateTimeOffset(2010, 1, 1, 1, 1, 1, TimeSpan.Zero)));
        }

        #region Helper methods

        private void CheckValidParsedValue(string input, DateTimeOffset expectedResult)
        {
            DateTimeOffset result;
            Assert.True(HeaderUtilities.TryParseDate(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParsedValue(string input)
        {
            DateTimeOffset result;
            Assert.False(HeaderUtilities.TryParseDate(input, out result));
            Assert.Equal(new DateTimeOffset(), result);
        }

        #endregion
    }
}
