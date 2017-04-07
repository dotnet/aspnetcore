// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Tests.TestHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class FrameResponseStreamTests
    {
        [Fact]
        public void CanReadReturnsFalse()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.False(stream.CanRead);
        }

        [Fact]
        public void CanSeekReturnsFalse()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void CanWriteReturnsTrue()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void ReadThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
        }

        [Fact]
        public void ReadByteThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.ReadByte());
        }

        [Fact]
        public async Task ReadAsyncThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(new byte[1], 0, 1));
        }

#if NET46
        [Fact]
        public void BeginReadThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.BeginRead(new byte[1], 0, 1, null, null));
        }
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif

        [Fact]
        public void SeekThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
        }

        [Fact]
        public void LengthThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.Length);
        }

        [Fact]
        public void SetLengthThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void PositionThrows()
        {
            var stream = new FrameResponseStream(new MockFrameControl());
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
        }
    }
}
