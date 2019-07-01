// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class UriBuildingContextTest
    {
        [Fact]
        public void EncodeValue_EncodesEntireValue_WhenEncodeSlashes_IsFalse()
        {
            // Arrange
            var urlTestEncoder = new UrlTestEncoder();
            var value = "a/b b1/c";
            var expected = "/UrlEncode[[a/b b1/c]]";
            var uriBuilldingContext = new UriBuildingContext(urlTestEncoder);

            // Act
            uriBuilldingContext.EncodeValue(value, 0, value.Length, encodeSlashes: true);

            // Assert
            Assert.Equal(expected, uriBuilldingContext.ToString());
        }

        [Fact]
        public void EncodeValue_EncodesOnlySlashes_WhenEncodeSlashes_IsFalse()
        {
            // Arrange
            var urlTestEncoder = new UrlTestEncoder();
            var value = "a/b b1/c";
            var expected = "/UrlEncode[[a]]/UrlEncode[[b b1]]/UrlEncode[[c]]";
            var uriBuilldingContext = new UriBuildingContext(urlTestEncoder);

            // Act
            uriBuilldingContext.EncodeValue(value, 0, value.Length, encodeSlashes: false);

            // Assert
            Assert.Equal(expected, uriBuilldingContext.ToString());
        }

        [Theory]
        [InlineData("a/b b1/c", 0, 2, "/UrlEncode[[a]]/")]
        [InlineData("a/b b1/c", 3, 4, "/UrlEncode[[ b1]]/")]
        [InlineData("a/b b1/c", 3, 5, "/UrlEncode[[ b1]]/UrlEncode[[c]]")]
        [InlineData("a/b b1/c/", 8, 1, "/")]
        [InlineData("/", 0, 1, "/")]
        [InlineData("/a", 0, 2, "/UrlEncode[[a]]")]
        [InlineData("a", 0, 1, "/UrlEncode[[a]]")]
        [InlineData("a/", 0, 2, "/UrlEncode[[a]]/")]
        public void EncodeValue_EncodesOnlySlashes_WithinSubsegment_WhenEncodeSlashes_IsFalse(
            string value,
            int startIndex,
            int characterCount,
            string expected)
        {
            // Arrange
            var urlTestEncoder = new UrlTestEncoder();
            var uriBuilldingContext = new UriBuildingContext(urlTestEncoder);

            // Act
            uriBuilldingContext.EncodeValue(value, startIndex, characterCount, encodeSlashes: false);

            // Assert
            Assert.Equal(expected, uriBuilldingContext.ToString());
        }

        [Theory]
        [InlineData("/Author", false, false, "/UrlEncode[[Author]]")]
        [InlineData("/Author", false, true, "/UrlEncode[[Author]]")]
        [InlineData("/Author", true, false, "/UrlEncode[[Author]]/")]
        [InlineData("/Author", true, true, "/UrlEncode[[Author]]/")]
        [InlineData("/Author/", false, false, "/UrlEncode[[Author]]/")]
        [InlineData("/Author/", false, true, "/UrlEncode[[Author/]]")]
        [InlineData("/Author/", true, false, "/UrlEncode[[Author]]/")]
        [InlineData("/Author/", true, true, "/UrlEncode[[Author/]]/")]
        [InlineData("Author", false, false, "/UrlEncode[[Author]]")]
        [InlineData("Author", false, true, "/UrlEncode[[Author]]")]
        [InlineData("Author", true, false, "/UrlEncode[[Author]]/")]
        [InlineData("Author", true, true, "/UrlEncode[[Author]]/")]
        [InlineData("", false, false, "")]
        [InlineData("", false, true, "")]
        [InlineData("", true, false, "")]
        [InlineData("", true, true, "")]
        public void ToPathString(string url, bool appendTrailingSlash, bool encodeSlashes, string expected)
        {
            // Arrange
            var urlTestEncoder = new UrlTestEncoder();
            var uriBuilldingContext = new UriBuildingContext(urlTestEncoder);
            uriBuilldingContext.AppendTrailingSlash = appendTrailingSlash;

            // Act
            uriBuilldingContext.Accept(url, encodeSlashes);

            // Assert
            Assert.Equal(expected, uriBuilldingContext.ToPathString().Value);
        }
    }
}
