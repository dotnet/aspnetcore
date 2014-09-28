// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class StringWithQualityHeaderValueParsingTests
    {
        [Theory]
        [InlineData("*", HttpHeaderUtilitites.Match, "*")]
        [InlineData("*", 0.7, "*;q=.7")]
        [InlineData("iso-8859-5", HttpHeaderUtilitites.Match, "iso-8859-5")]
        [InlineData("unicode-1-1", 0.8, "unicode-1-1;q=0.8")]
        [InlineData("unicode-1-1", 0.8, "unicode-1-1;q =0.8")]
        [InlineData("unicode-1-1", 0.8, "unicode-1-1;q = 0.8")]
        [InlineData("unicode-1-1", 1.0, "unicode-1-1;quxx = 0.8")] // quxx gets ignored.
        public void StringWithQualityHeaderValue_ParseSuccessfully(string value,
                                         double quality,
                                         string rawValue)
        {
            // Arrange
            var parsedValue = StringWithQualityHeaderValue.Parse(rawValue);

            // Act and Assert
            Assert.Equal(rawValue, parsedValue.RawValue);
            Assert.Equal(value, parsedValue.Value);
            Assert.Equal(quality, parsedValue.Quality);
        }

        [Theory]
        [InlineData("*", true, 0.7, "*;q=.7")]
        [InlineData("iso-8859-5", true, HttpHeaderUtilitites.Match, "iso-8859-5")]
        [InlineData("unicode-1-1", true, 0.8, "unicode-1-1;q=0.8")]
        [InlineData("unicode-1-1", false, 0.8, "unicode-1-1;q=-")]
        public void StringWithQualityHeaderValue_TryParse_ReturnsApproperiateResults(
            string value,
            bool result,
            double quality,
            string rawValue)
        {
            // Arrange
            StringWithQualityHeaderValue parsedValue;
            var isValid = StringWithQualityHeaderValue.TryParse(rawValue, out parsedValue);

            // Act and Assert
            Assert.Equal(result, isValid);
            if(result == true)
            {
                Assert.Equal(rawValue, parsedValue.RawValue);
                Assert.Equal(value, parsedValue.Value);
                Assert.Equal(quality, parsedValue.Quality);
            }
        }
    }
}
