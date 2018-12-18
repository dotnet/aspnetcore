// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ChunkWriterTests
    {
        [Theory]
        [InlineData(0, "0\r\n")]
        [InlineData(1, "1\r\n")]
        [InlineData(10, "a\r\n")]
        [InlineData(0x08, "8\r\n")]
        [InlineData(0x10, "10\r\n")]
        [InlineData(0x080, "80\r\n")]
        [InlineData(0x100, "100\r\n")]
        [InlineData(0x0800, "800\r\n")]
        [InlineData(0x1000, "1000\r\n")]
        [InlineData(0x08000, "8000\r\n")]
        [InlineData(0x10000, "10000\r\n")]
        [InlineData(0x080000, "80000\r\n")]
        [InlineData(0x100000, "100000\r\n")]
        [InlineData(0x0800000, "800000\r\n")]
        [InlineData(0x1000000, "1000000\r\n")]
        [InlineData(0x08000000, "8000000\r\n")]
        [InlineData(0x10000000, "10000000\r\n")]
        [InlineData(0x7fffffffL, "7fffffff\r\n")]
        public void ChunkedPrefixMustBeHexCrLfWithoutLeadingZeros(int dataCount, string expected)
        {
            Span<byte> span = new byte[10];
            var count = ChunkWriter.BeginChunkBytes(dataCount, span);

            Assert.Equal(Encoding.ASCII.GetBytes(expected), span.Slice(0, count).ToArray());
        }
    }
}
