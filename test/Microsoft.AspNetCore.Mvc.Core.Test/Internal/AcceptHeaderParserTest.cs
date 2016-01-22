// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Internal
{
    public class AcceptHeaderParserTest
    {
        [Fact]
        public void ParseAcceptHeader_ParsesSimpleHeader()
        {
            // Arrange
            var header = "application/json";
            var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0)
            };

            // Act
            var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string> { header });

            // Assert
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void ParseAcceptHeader_ParsesSimpleHeaderWithMultipleValues()
        {
            // Arrange
            var header = "application/json, application/xml;q=0.8";
            var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/xml;q=0.8"),0.8)
            };

            // Act
            var parsed = AcceptHeaderParser.ParseAcceptHeader(new List<string> { header });

            // Assert
            Assert.Equal(expected, parsed);
            foreach (var mediaType in parsed)
            {
                Assert.Same(header, mediaType.MediaType.Buffer);
            }
        }

        [Fact]
        public void ParseAcceptHeader_ParsesMultipleHeaderValues()
        {
            // Arrange
            var expected = new List<MediaTypeSegmentWithQuality>
            {
                new MediaTypeSegmentWithQuality(new StringSegment("application/json"),1.0),
                new MediaTypeSegmentWithQuality(new StringSegment("application/xml;q=0.8"),0.8)
            };

            // Act
            var parsed = AcceptHeaderParser.ParseAcceptHeader(
                new List<string> { "application/json", "", "application/xml;q=0.8" });

            // Assert
            Assert.Equal(expected, parsed);
        }
    }
}
