// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.HeaderValueAbstractions
{
    public class MediaTypeHeaderValueParsingTests
    {
        public static IEnumerable<object[]> GetValidMediaTypeWithQualityHeaderValues
        {
            get
            {
                yield return new object[]
                {
                    "*",
                    "*",
                    null,
                    MediaTypeHeaderValueRange.AllMediaRange,
                    new Dictionary<string, string>(),
                    HttpHeaderUtilitites.Match,
                    "*/*"
                };

                yield return new object[]
                {
                    "text",
                    "*",
                    "utf-8",
                    MediaTypeHeaderValueRange.SubtypeMediaRange,
                    new Dictionary<string, string>() { { "charset", "utf-8" }, { "foo", "bar" } },
                    HttpHeaderUtilitites.Match,
                    "text/*;charset=utf-8;foo=bar",
                };

                yield return new object[]
                {
                    "text",
                    "plain",
                    "utf-8",
                    MediaTypeHeaderValueRange.None,
                    new Dictionary<string, string>() { { "charset", "utf-8" }, { "foo", "bar" }, { "q", "0.0" } },
                    HttpHeaderUtilitites.NoMatch,
                    "text/plain;charset=utf-8;foo=bar;q=0.0",
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetValidMediaTypeWithQualityHeaderValues))]
        public void MediaTypeWithQualityHeaderValue_ParseSuccessfully(string mediaType,
                                         string mediaSubType,
                                         string charset,
                                         MediaTypeHeaderValueRange range,
                                         IDictionary<string, string> parameters,
                                         double quality,
                                         string rawValue)
        {
            var parsedValue = MediaTypeWithQualityHeaderValue.Parse(rawValue);
            // Act and Assert
            Assert.Equal(rawValue, parsedValue.RawValue);
            Assert.Equal(mediaType, parsedValue.MediaType);
            Assert.Equal(mediaSubType, parsedValue.MediaSubType);
            Assert.Equal(charset, parsedValue.Charset);
            Assert.Equal(range, parsedValue.MediaTypeRange);
            ValidateParametes(parameters, parsedValue.Parameters);
        }

        [Theory]
        [InlineData("*/*;", "*/*", true)]
        [InlineData("text/*;", "text/*", true)]
        [InlineData("text/plain;", "text/plain", true)]
        [InlineData("*/*;", "*/*;charset=utf-8;", true)]
        [InlineData("text/*;", "*/*;charset=utf-8;", true)]
        [InlineData("text/plain;", "*/*;charset=utf-8;", true)]
        [InlineData("text/plain;", "text/*;charset=utf-8;", true)]
        [InlineData("text/plain;", "text/plain;charset=utf-8;", true)]
        [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/plain;charset=utf-8;foo=bar;q=0.0", true)]
        [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "text/*;charset=utf-8;foo=bar;q=0.0", true)]
        [InlineData("text/plain;charset=utf-8;foo=bar;q=0.0", "*/*;charset=utf-8;foo=bar;q=0.0", true)]
        [InlineData("*/*;", "text/plain;charset=utf-8;foo=bar;q=0.0", false)]
        [InlineData("text/*;", "text/plain;charset=utf-8;foo=bar;q=0.0", false)]
        [InlineData("text/plain;missingparam=4;", "text/plain;charset=utf-8;foo=bar;q=0.0", false)]
        [InlineData("text/plain;missingparam=4;", "text/*;charset=utf-8;foo=bar;q=0.0", false)]
        [InlineData("text/plain;missingparam=4;", "*/*;charset=utf-8;foo=bar;q=0.0", false)]
        public void MediaTypeHeaderValue_IsSubTypeTests(string mediaType1,
                                                        string mediaType2,
                                                        bool isMediaType1Subset)
        {
            var parsedMediaType1 = MediaTypeWithQualityHeaderValue.Parse(mediaType1);
            var parsedMediaType2 = MediaTypeWithQualityHeaderValue.Parse(mediaType2);

            // Act
            var isSubset = parsedMediaType1.IsSubsetOf(parsedMediaType2);

            // Assert
            Assert.Equal(isMediaType1Subset, isSubset);
        }

        [Theory]
        [InlineData("text/plain;charset=utf-16;foo=bar", "text/plain;charset=utf-8;foo=bar")]
        [InlineData("text/plain;charset=utf-16;foo=bar", "text/plain;charset=utf-16;foo=bar1")]
        [InlineData("text/plain;charset=utf-16;foo=bar", "text/json;charset=utf-16;foo=bar")]
        [InlineData("text/plain;charset=utf-16;foo=bar", "application/plain;charset=utf-16;foo=bar")]
        [InlineData("text/plain;charset=utf-16;foo=bar", "application/json;charset=utf-8;foo=bar1")]
        public void MediaTypeHeaderValue_UpdateValue_RawValueGetsUpdated(string mediaTypeValue,
                                                        string expectedRawValue)
        {
            // Arrange
            var parsedOldValue = MediaTypeHeaderValue.Parse(mediaTypeValue);
            var parsedNewValue = MediaTypeHeaderValue.Parse(expectedRawValue);

            // Act
            parsedOldValue.Charset = parsedNewValue.Charset;
            parsedOldValue.Parameters = parsedNewValue.Parameters;
            parsedOldValue.MediaType = parsedNewValue.MediaType;
            parsedOldValue.MediaSubType = parsedNewValue.MediaSubType;
            parsedOldValue.MediaTypeRange = parsedNewValue.MediaTypeRange;

            // Assert
            Assert.Equal(expectedRawValue, parsedOldValue.RawValue);
        }

        private static void ValidateParametes(IDictionary<string, string> expectedParameters,
                                              IDictionary<string, string> actualParameters)
        {
            Assert.Equal(expectedParameters.Count, actualParameters.Count);
            foreach (var key in expectedParameters.Keys)
            {
                Assert.Equal(expectedParameters[key], actualParameters[key]);
            }
        }
    }
}
