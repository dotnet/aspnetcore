// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Http.Abstractions
{
    public class HttpProtocolsTests
    {
        [Fact]
        public void Http3_Success()
        {
            Assert.Equal("HTTP/3", HttpProtocols.Http3);
        }

        [Theory]
        [InlineData("HTTP/3", true)]
        [InlineData("http/3", false)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/3.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/3", false)]
        [InlineData("HTTP/3 ", false)]
        public void IsHttp3_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocols.IsHttp3(protocol));
        }

        [Fact]
        public void Http2_Success()
        {
            Assert.Equal("HTTP/2", HttpProtocols.Http2);
        }

        [Theory]
        [InlineData("HTTP/2", true)]
        [InlineData("http/2", false)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/2.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/2", false)]
        [InlineData("HTTP/2 ", false)]
        public void IsHttp2_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocols.IsHttp2(protocol));
        }

        [Fact]
        public void Http11_Success()
        {
            Assert.Equal("HTTP/1.1", HttpProtocols.Http11);
        }

        [Theory]
        [InlineData("HTTP/1.1", true)]
        [InlineData("http/1.1", false)]
        [InlineData("HTTP/2", false)]
        [InlineData("HTTP/1.0", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/1.1", false)]
        [InlineData("HTTP/1.1 ", false)]
        public void IsHttp11_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocols.IsHttp11(protocol));
        }

        [Fact]
        public void Http10_Success()
        {
            Assert.Equal("HTTP/1.0", HttpProtocols.Http10);
        }

        [Theory]
        [InlineData("HTTP/1.0", true)]
        [InlineData("http/1.0", false)]
        [InlineData("HTTP/2", false)]
        [InlineData("HTTP/1.1", false)]
        [InlineData("HTTP/1", false)]
        [InlineData(" HTTP/1.0", false)]
        [InlineData("HTTP/1.0 ", false)]
        public void IsHttp10_Success(string protocol, bool match)
        {
            Assert.Equal(match, HttpProtocols.IsHttp10(protocol));
        }
    }
}
