// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Internal
{
    public class RangeHelperTests
    {
        [Fact]
        public void NormalizeRanges_ReturnsEmptyArrayWhenRangeCountZero()
        {
            // Arrange
            var ranges = new List<RangeItemHeaderValue>();

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 2);

            // Assert
            Assert.Empty(normalizedRanges);
        }

        [Fact]
        public void NormalizeRanges_ReturnsEmptyArrayWhenLengthZero()
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(0, 0),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 0);

            // Assert
            Assert.Empty(normalizedRanges);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 3)]
        public void NormalizeRanges_SkipsItemWhenRangeStartEqualOrGreaterThanLength(long start, long end)
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(start, end),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 1);

            // Assert
            Assert.Empty(normalizedRanges);
        }

        [Fact]
        public void NormalizeRanges_SkipsItemWhenRangeEndEqualsZero()
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(null, 0),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 1);

            // Assert
            Assert.Empty(normalizedRanges);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 0)]
        [InlineData(0, null)]
        [InlineData(0, 0)]
        public void NormalizeRanges_ReturnsNormalizedRange(long start, long end)
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(start, end),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 1);

            // Assert
            var range = Assert.Single(normalizedRanges);
            Assert.Equal(0, range.From);
            Assert.Equal(0, range.To);
        }

        [Fact]
        public void NormalizeRanges_ReturnsRangeWithNoChange()
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(1, 3),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 4);

            // Assert
            var range = Assert.Single(normalizedRanges);
            Assert.Equal(1, range.From);
            Assert.Equal(3, range.To);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, 0)]
        [InlineData(0, null)]
        [InlineData(0, 0)]
        public void NormalizeRanges_MultipleRanges_ReturnsNormalizedRange(long start, long end)
        {
            // Arrange
            var ranges = new[]
            {
                new RangeItemHeaderValue(start, end),
                new RangeItemHeaderValue(1, 2),
            };

            // Act
            var normalizedRanges = RangeHelper.NormalizeRanges(ranges, 3);

            // Assert
            Assert.Collection(normalizedRanges,
                range =>
                {
                    Assert.Equal(0, range.From);
                    Assert.Equal(0, range.To);
                },
                range =>
                {
                    Assert.Equal(1, range.From);
                    Assert.Equal(2, range.To);
                });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ParseRange_ReturnsNullWhenRangeHeaderNotProvided(string range)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[HeaderNames.Range] = range;

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders(), new DateTimeOffset(), null);

            // Assert
            Assert.Null(parsedRangeResult);
        }

        [Theory]
        [InlineData("1-2, 3-4")]
        [InlineData("1-2, ")]
        public void ParseRange_ReturnsNullWhenMultipleRangesProvidedInRangeHeader(string range)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[HeaderNames.Range] = range;

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders(), new DateTimeOffset(), null);

            // Assert
            Assert.Null(parsedRangeResult);
        }

        [Fact]
        public void ParseRange_ReturnsNullWhenLastModifiedGreaterThanIfRangeHeaderLastModified()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var range = new RangeHeaderValue(1, 2);
            httpContext.Request.Headers[HeaderNames.Range] = range.ToString();
            var lastModified = new RangeConditionHeaderValue(DateTime.Now);
            httpContext.Request.Headers[HeaderNames.IfRange] = lastModified.ToString();

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders(), DateTime.Now.AddMilliseconds(2), null);

            // Assert
            Assert.Null(parsedRangeResult);
        }

        [Fact]
        public void ParseRange_ReturnsNullWhenETagNotEqualToIfRangeHeaderEntityTag()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var range = new RangeHeaderValue(1, 2);
            httpContext.Request.Headers[HeaderNames.Range] = range.ToString();
            var etag = new RangeConditionHeaderValue("\"tag\"");
            httpContext.Request.Headers[HeaderNames.IfRange] = etag.ToString();

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders(), DateTime.Now, new EntityTagHeaderValue("\"etag\""));

            // Assert
            Assert.Null(parsedRangeResult);
        }

        [Fact]
        public void ParseRange_ReturnsSingleRangeWhenInputValid()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var range = new RangeHeaderValue(1, 2);
            httpContext.Request.Headers[HeaderNames.Range] = range.ToString();
            var lastModified = new RangeConditionHeaderValue(DateTime.Now);
            httpContext.Request.Headers[HeaderNames.IfRange] = lastModified.ToString();
            var etag = new RangeConditionHeaderValue("\"etag\"");
            httpContext.Request.Headers[HeaderNames.IfRange] = etag.ToString();

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders(), DateTime.Now, new EntityTagHeaderValue("\"etag\""));

            // Assert
            var parsedRange = Assert.Single(parsedRangeResult);
            Assert.Equal(1, parsedRange.From);
            Assert.Equal(2, parsedRange.To);
        }

        [Fact]
        public void ParseRange_ReturnsRangeWhenLastModifiedAndEtagNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var range = new RangeHeaderValue(1, 2);
            httpContext.Request.Headers[HeaderNames.Range] = range.ToString();
            var lastModified = new RangeConditionHeaderValue(DateTime.Now);
            httpContext.Request.Headers[HeaderNames.IfRange] = lastModified.ToString();

            // Act
            var parsedRangeResult = RangeHelper.ParseRange(httpContext, httpContext.Request.GetTypedHeaders());

            // Assert
            var parsedRange = Assert.Single(parsedRangeResult);
            Assert.Equal(1, parsedRange.From);
            Assert.Equal(2, parsedRange.To);
        }
    }
}
