// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class KestrelServerLimitsTests
    {
        [Fact]
        public void MaxResponseBufferSizeDefault()
        {
            Assert.Equal(64 * 1024, (new KestrelServerLimits()).MaxResponseBufferSize);
        }

        [Theory]
        [InlineData((long)-1)]
        [InlineData(long.MinValue)]
        public void MaxResponseBufferSizeInvalid(long value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (new KestrelServerLimits()).MaxResponseBufferSize = value;
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData((long)0)]
        [InlineData((long)1)]
        [InlineData(long.MaxValue)]
        public void MaxResponseBufferSizeValid(long? value)
        {
            var o = new KestrelServerLimits();
            o.MaxResponseBufferSize = value;
            Assert.Equal(value, o.MaxResponseBufferSize);
        }

        [Fact]
        public void MaxRequestBufferSizeDefault()
        {
            Assert.Equal(1024 * 1024, (new KestrelServerLimits()).MaxRequestBufferSize);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void MaxRequestBufferSizeInvalid(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (new KestrelServerLimits()).MaxRequestBufferSize = value;
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void MaxRequestBufferSizeValid(int? value)
        {
            var o = new KestrelServerLimits();
            o.MaxRequestBufferSize = value;
            Assert.Equal(value, o.MaxRequestBufferSize);
        }

        [Fact]
        public void MaxRequestLineSizeDefault()
        {
            Assert.Equal(8 * 1024, (new KestrelServerLimits()).MaxRequestLineSize);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void MaxRequestLineSizeInvalid(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (new KestrelServerLimits()).MaxRequestLineSize = value;
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void MaxRequestLineSizeValid(int value)
        {
            var o = new KestrelServerLimits();
            o.MaxRequestLineSize = value;
            Assert.Equal(value, o.MaxRequestLineSize);
        }

        [Fact]
        public void MaxRequestHeaderTotalSizeDefault()
        {
            Assert.Equal(32 * 1024, (new KestrelServerLimits()).MaxRequestHeadersTotalSize);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void MaxRequestHeaderTotalSizeInvalid(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (new KestrelServerLimits()).MaxRequestHeadersTotalSize = value;
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void MaxRequestHeaderTotalSizeValid(int value)
        {
            var o = new KestrelServerLimits();
            o.MaxRequestHeadersTotalSize = value;
            Assert.Equal(value, o.MaxRequestHeadersTotalSize);
        }

        [Fact]
        public void MaxRequestHeaderCountDefault()
        {
            Assert.Equal(100, (new KestrelServerLimits()).MaxRequestHeaderCount);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public void MaxRequestHeaderCountInvalid(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (new KestrelServerLimits()).MaxRequestHeaderCount = value;
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void MaxRequestHeaderCountValid(int value)
        {
            var o = new KestrelServerLimits();
            o.MaxRequestHeaderCount = value;
            Assert.Equal(value, o.MaxRequestHeaderCount);
        }

        [Fact]
        public void KeepAliveTimeoutDefault()
        {
            Assert.Equal(TimeSpan.FromMinutes(2), new KestrelServerLimits().KeepAliveTimeout);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0.5)]
        [InlineData(2.1)]
        [InlineData(2.5)]
        [InlineData(2.9)]
        public void KeepAliveTimeoutIsRoundedToTheNextSecond(double seconds)
        {
            var o = new KestrelServerLimits();
            o.KeepAliveTimeout = TimeSpan.FromSeconds(seconds);
            Assert.Equal(Math.Ceiling(seconds), o.KeepAliveTimeout.TotalSeconds);
        }
    }
}
