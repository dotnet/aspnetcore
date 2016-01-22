// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class MediaTypeTest
    {
        [Theory]
        [InlineData("application/json")]
        [InlineData("application /json")]
        public void CanParse_ParameterlessMediaTypes(string mediaType)
        {
            // Arrange & Act
            var result = new MediaType(mediaType, 0, mediaType.Length);

            // Assert
            Assert.Equal(new StringSegment("application"), result.Type);
            Assert.Equal(new StringSegment("json"), result.SubType);
        }

        [Theory]
        [InlineData("application/json;format=pretty;charset=utf-8;q=0.8")]
        [InlineData("application/json;format=pretty;charset=utf-8; q=0.8 ")]
        [InlineData("application/json;format=pretty;charset=utf-8 ; q=0.8 ")]
        [InlineData("application/json;format=pretty; charset=utf-8 ; q=0.8 ")]
        [InlineData("application/json;format=pretty ; charset=utf-8 ; q=0.8 ")]
        [InlineData("application/json; format=pretty ; charset=utf-8 ; q=0.8 ")]
        [InlineData("application/json; format=pretty ; charset=utf-8 ; q=  0.8 ")]
        [InlineData("application/json; format=pretty ; charset=utf-8 ; q  =  0.8 ")]
        public void CanParse_MediaTypesWithParameters(string mediaType)
        {
            // Arrange & Act
            var result = new MediaType(mediaType, 0, mediaType.Length);

            // Assert
            Assert.Equal(new StringSegment("application"), result.Type);
            Assert.Equal(new StringSegment("json"), result.SubType);
            Assert.Equal(new StringSegment("pretty"), result.GetParameter("format"));
            Assert.Equal(new StringSegment("0.8"), result.GetParameter("q"));
            Assert.Equal(new StringSegment("utf-8"), result.GetParameter("charset"));
        }

        [Theory]
        [InlineData("application/json;charset=utf-8")]
        [InlineData("application/json;format=indent;q=0.8;charset=utf-8")]
        [InlineData("application/json;format=indent;charset=utf-8;q=0.8")]
        [InlineData("application/json;charset=utf-8;format=indent;q=0.8")]
        public void GetParameter_ReturnsParameter_IfParameterIsInMediaType(string mediaType)
        {
            // Arrange
            var expectedParameter = new StringSegment("utf-8");

            var parsedMediaType = new MediaType(mediaType, 0, mediaType.Length);

            // Act
            var result = parsedMediaType.GetParameter("charset");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedParameter, result);
        }

        [Fact]
        public void GetParameter_IsCaseInsensitive()
        {
            // Arrange
            var mediaType = "application/json;charset=utf-8";
            var expectedParameter = new StringSegment("utf-8");

            var parsedMediaType = new MediaType(mediaType);

            // Act
            var result = parsedMediaType.GetParameter("CHARSET");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedParameter, result);
        }

        [Theory]
        [InlineData("application/json", "application/json", true)]
        [InlineData("application/json", "application/json;charset=utf-8", true)]
        [InlineData("application/json;charset=utf-8", "application/json", false)]
        [InlineData("application/json;q=0.8", "application/json;q=0.9", true)]
        [InlineData("application/json;q=0.8;charset=utf-7", "application/json;charset=utf-8;q=0.9", true)]
        [InlineData("application/json;format=indent;charset=utf-8", "application/json", false)]
        [InlineData("application/json", "application/json;format=indent;charset=utf-8", true)]
        [InlineData("application/json;format=indent;charset=utf-8", "application/json;format=indent;charset=utf-8", true)]
        [InlineData("application/json;charset=utf-8;format=indent", "application/json;format=indent;charset=utf-8", true)]
        public void IsSubsetOf(string set, string subset, bool expectedResult)
        {
            // Arrange
            var setMediaType = new MediaType(set);
            var subSetMediaType = new MediaType(subset);

            // Act
            var result = subSetMediaType.IsSubsetOf(setMediaType);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("*/*", true)]
        [InlineData("text/*", false)]
        [InlineData("text/plain", false)]
        public void MatchesAllTypes(string value, bool expectedResult)
        {
            // Arrange
            var mediaType = new MediaType(value);

            // Act
            var result = mediaType.MatchesAllTypes;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("*/*", true)]
        [InlineData("text/*", true)]
        [InlineData("text/plain", false)]
        public void MatchesAllSubtypes(string value, bool expectedResult)
        {
            // Arrange
            var mediaType = new MediaType(value);

            // Act
            var result = mediaType.MatchesAllSubTypes;

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void GetParameter_ReturnsNull_IfParameterIsNotInMediaType()
        {
            var mediaType = "application/json;charset=utf-8;format=indent;q=0.8";

            var parsedMediaType = new MediaType(mediaType, 0, mediaType.Length);

            // Act
            var result = parsedMediaType.GetParameter("other");

            // Assert
            Assert.False(result.HasValue);
        }
    }
}
