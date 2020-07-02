// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class HeaderAttributeTests
    {
        [Fact]
        public void Constructor_AppliedDefaultMatchOptions()
        {
            // Act
            var sut = new HeaderAttribute("some-header");

            // Assert
            Assert.Equal("some-header", sut.HeaderName);
            Assert.Equal(HeaderValueMatchMode.Exact, sut.ValueMatchMode);
            Assert.False(sut.ValueIgnoresCase);
        }

        [Fact]
        public void Constructor_MatchOptions()
        {
            // Act
            var sut = new HeaderAttribute("some-header", "abc")
            {
                ValueMatchMode = HeaderValueMatchMode.Prefix,
                ValueIgnoresCase = true,
            };

            // Assert
            Assert.Equal(HeaderValueMatchMode.Prefix, sut.ValueMatchMode);
            Assert.True(sut.ValueIgnoresCase);
        }

        [Fact]
        public void Constructor_OnlyHeaderName_Works()
        {
            // Act
            var sut = new HeaderAttribute("some-header");

            // Assert
            Assert.Equal("some-header", sut.HeaderName);
            Assert.Empty(sut.HeaderValues);
        }

        [Fact]
        public void Constructor_HeaderNameAndValue_Works()
        {
            // Act
            var sut = new HeaderAttribute("some-header", "abc");

            // Assert
            Assert.Equal("some-header", sut.HeaderName);
            Assert.Equal(new[] { "abc" }, sut.HeaderValues);
        }

        [Fact]
        public void Constructor_HeaderNameAndValues_Works()
        {
            // Act
            var sut = new HeaderAttribute("some-header", new[] { "abc", "def" });

            // Assert
            Assert.Equal("some-header", sut.HeaderName);
            Assert.Equal(new[] { "abc", "def" }, sut.HeaderValues);
        }

        [Fact]
        public void Constructor_HeaderNameAndEmptyValues_Works()
        {
            // Act
            var sut = new HeaderAttribute("some-header", new string[0]);

            // Assert
            Assert.Equal("some-header", sut.HeaderName);
            Assert.Empty(sut.HeaderValues);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_NullHeaderName_Throws(string headerName)
        {
            // Act
            Action action = () => new HeaderAttribute(headerName);

            // Assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Constructor_NullHeaderValues_Throws()
        {
            // Act
            Action action = () => new HeaderAttribute("some-header", (string[])null);

            // Assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
