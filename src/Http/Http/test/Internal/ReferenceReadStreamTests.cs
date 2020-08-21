// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Http
{
    public class ReferenceReadStreamTests
    {
        [Fact]
        public void CanRead_ReturnsTrue()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CanSeek_ReturnsFalse()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.False(stream.CanSeek);
        }

        [Fact]
        public void CanWrite_ReturnsFalse()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public void SetLength_Throws()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
        }

        [Fact]
        public void Write_Throws()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
        }

        [Fact]
        public void WriteByte_Throws()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(0));
        }

        [Fact]
        public async Task WriteAsync_Throws()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[1], 0, 1));
        }

        [Fact]
        public void Flush_DoesNotThrow()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            stream.Flush();
        }

        [Fact]
        public async Task FlushAsync_DoesNotThrow()
        {
            var stream = new ReferenceReadStream(Mock.Of<Stream>(), 0, 1);
            await stream.FlushAsync();
        }
    }
}
