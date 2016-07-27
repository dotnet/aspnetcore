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
    }
}
