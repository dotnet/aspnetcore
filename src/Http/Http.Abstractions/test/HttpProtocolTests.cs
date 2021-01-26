// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Http.Abstractions
{
    public class HttpProtocolTests
    {
        [Fact]
        public void Http3_Success()
        {
            Assert.Equal("HTTP/3", HttpProtocol.Http3);
        }

        [Theory]
        [InlineData("HTTP/3", true)]
        [InlineData("http/3", true)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/3.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/3", false)]
        [InlineData("HTTP/3 ", false)]
        public void IsHttp3_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocol.IsHttp3(protocol));
        }

        [Fact]
        public void Http2_Success()
        {
            Assert.Equal("HTTP/2", HttpProtocol.Http2);
        }

        [Theory]
        [InlineData("HTTP/2", true)]
        [InlineData("http/2", true)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/2.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/2", false)]
        [InlineData("HTTP/2 ", false)]
        public void IsHttp2_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocol.IsHttp2(protocol));
        }

        [Fact]
        public void Http11_Success()
        {
            Assert.Equal("HTTP/1.1", HttpProtocol.Http11);
        }

        [Theory]
        [InlineData("HTTP/1.1", true)]
        [InlineData("http/1.1", true)]
        [InlineData("HTTP/2", false)]
        [InlineData("HTTP/1.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/1.1", false)]
        [InlineData("HTTP/1.1 ", false)]
        public void IsHttp11_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocol.IsHttp11(protocol));
        }

        [Fact]
        public void Http10_Success()
        {
            Assert.Equal("HTTP/1.0", HttpProtocol.Http10);
        }

        [Theory]
        [InlineData("HTTP/1.0", true)]
        [InlineData("http/1.0", true)]
        [InlineData("HTTP/2", false)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/1.0", false)]
        [InlineData("HTTP/1.0 ", false)]
        public void IsHttp10_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocol.IsHttp10(protocol));
        }

        public static TheoryData<Version, string> s_ValidData = new TheoryData<Version, string>
        {
            { new Version(3, 0), "HTTP/3" },
            { new Version(2, 0), "HTTP/2" },
            { new Version(1, 1), "HTTP/1.1" },
            { new Version(1, 0), "HTTP/1.0" }
        };

        [Theory]
        [MemberData(nameof(s_ValidData))]
        public void GetHttpProtocol_CorrectIETFVersion(Version version, string expected)
        {
            var actual = HttpProtocol.GetHttpProtocol(version);

            Assert.Equal(expected, actual);
        }

        public static TheoryData<Version> s_InvalidData = new TheoryData<Version>
        {
            { new Version(0, 3) },
            { new Version(2, 1) }
        };

        [Theory]
        [MemberData(nameof(s_InvalidData))]
        public void GetHttpProtocol_ThrowErrorForUnknownVersion(Version version)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HttpProtocol.GetHttpProtocol(version));
        }
    }
}
