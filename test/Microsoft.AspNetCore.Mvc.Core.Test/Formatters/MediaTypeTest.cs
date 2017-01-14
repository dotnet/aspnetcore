// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class MediaTypeTest
    {
        [Theory]
        [InlineData("application/json")]
        [InlineData("application /json")]
        [InlineData(" application / json ")]
        public void Constructor_CanParseParameterlessMediaTypes(string mediaType)
        {
            // Arrange & Act
            var result = new MediaType(mediaType, 0, mediaType.Length);

            // Assert
            Assert.Equal(new StringSegment("application"), result.Type);
            Assert.Equal(new StringSegment("json"), result.SubType);
        }

        public static TheoryData<string> MediaTypesWithParameters
        {
            get
            {
                return new TheoryData<string>
                {
                    "application/json;format=pretty;charset=utf-8;q=0.8",
                    "application/json;format=pretty;charset=\"utf-8\";q=0.8",
                    "application/json;format=pretty;charset=utf-8; q=0.8 ",
                    "application/json;format=pretty;charset=utf-8 ; q=0.8 ",
                    "application/json;format=pretty; charset=utf-8 ; q=0.8 ",
                    "application/json;format=pretty ; charset=utf-8 ; q=0.8 ",
                    "application/json; format=pretty ; charset=utf-8 ; q=0.8 ",
                    "application/json; format=pretty ; charset=utf-8 ; q=  0.8 ",
                    "application/json; format=pretty ; charset=utf-8 ; q  =  0.8 ",
                    " application /  json; format =  pretty ; charset = utf-8 ; q  =  0.8 ",
                    " application /  json; format =  \"pretty\" ; charset = \"utf-8\" ; q  =  \"0.8\" ",
                };
            }
        }

        [Theory]
        [MemberData(nameof(MediaTypesWithParameters))]
        public void Constructor_CanParseMediaTypesWithParameters(string mediaType)
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

        [Fact]
        public void Constructor_NullLength_IgnoresLength()
        {
            // Arrange & Act
            var result = new MediaType("mediaType", 1, length: null);

            // Assert
            Assert.Equal(new StringSegment("ediaType"), result.Type);
        }

        [Fact]
        public void Constructor_NullMediaType_Throws()
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentNullException>("mediaType", () => new MediaType(null, 0, 2));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void Constructor_NegativeOffset_Throws(int offset)
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => new MediaType("media", offset, 5));
        }

        [Fact]
        public void Constructor_NegativeLength_Throws()
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>("length", () => new MediaType("media", 0, -1));
        }

        [Fact]
        public void Constructor_OffsetOrLengthOutOfBounds_Throws()
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentException>(() => new MediaType("lengthof9", 5, 5));
        }

        [Theory]
        [MemberData(nameof(MediaTypesWithParameters))]
        public void ReplaceEncoding_ReturnsExpectedMediaType(string mediaType)
        {
            // Arrange
            var encoding = Encoding.GetEncoding("iso-8859-1");
            var expectedMediaType = mediaType.Replace("utf-8", "iso-8859-1");

            // Act
            var result = MediaType.ReplaceEncoding(mediaType, encoding);

            // Assert
            Assert.Equal(expectedMediaType, result);
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
            Assert.Equal(expectedParameter, result);
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
            Assert.Equal(expectedParameter, result);
        }

        [Theory]
        [InlineData("application/json", "application/json")]
        [InlineData("application/json", "application/json;charset=utf-8")]
        [InlineData("application/json;q=0.8", "application/json;q=0.9")]
        [InlineData("application/json;q=0.8;charset=utf-7", "application/json;charset=utf-8;q=0.9")]
        [InlineData("application/json", "application/json;format=indent;charset=utf-8")]
        [InlineData("application/json;format=indent;charset=utf-8", "application/json;format=indent;charset=utf-8")]
        [InlineData("application/json;charset=utf-8;format=indent", "application/json;format=indent;charset=utf-8")]
        public void IsSubsetOf_ReturnsTrueWhenExpected(string set, string subset)
        {
            // Arrange
            var setMediaType = new MediaType(set);
            var subSetMediaType = new MediaType(subset);

            // Act
            var result = subSetMediaType.IsSubsetOf(setMediaType);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("application/json;charset=utf-8", "application/json")]
        [InlineData("application/json;format=indent;charset=utf-8", "application/json")]
        [InlineData("application/json;format=indent;charset=utf-8", "application/json;charset=utf-8")]
        public void IsSubsetOf_ReturnsFalseWhenExpected(string set, string subset)
        {
            // Arrange
            var setMediaType = new MediaType(set);
            var subSetMediaType = new MediaType(subset);

            // Act
            var result = subSetMediaType.IsSubsetOf(setMediaType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void MatchesAllTypes_ReturnsTrueWhenExpected()
        {
            // Arrange
            var mediaType = new MediaType("*/*");

            // Act
            var result = mediaType.MatchesAllTypes;

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("text/*")]
        [InlineData("text/plain")]
        public void MatchesAllTypes_ReturnsFalseWhenExpected(string value)
        {
            // Arrange
            var mediaType = new MediaType(value);

            // Act
            var result = mediaType.MatchesAllTypes;

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("*/*")]
        [InlineData("text/*")]
        public void MatchesAllSubtypes_ReturnsTrueWhenExpected(string value)
        {
            // Arrange
            var mediaType = new MediaType(value);

            // Act
            var result = mediaType.MatchesAllSubTypes;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void MatchesAllSubtypes_ReturnsFalseWhenExpected()
        {
            // Arrange
            var mediaType = new MediaType("text/plain");

            // Act
            var result = mediaType.MatchesAllSubTypes;

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MediaTypesWithParameters))]
        [InlineData("application/json;format=pretty;q=0.9;charset=utf-8;q=0.8")]
        [InlineData("application/json;format=pretty;q=0.9;charset=utf-8;q=0.8;version=3")]
        public void CreateMediaTypeSegmentWithQuality_FindsQValue(string value)
        {
            // Arrange & Act
            var mediaTypeSegment = MediaType.CreateMediaTypeSegmentWithQuality(value, start: 0);

            // Assert
            Assert.Equal(0.8d, mediaTypeSegment.Quality);
        }
    }
}
