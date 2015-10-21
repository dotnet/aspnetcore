// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class FrameTests
    {
        [Theory]
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
            var beginChunkBytes = Frame.BeginChunkBytes(dataCount);

            Assert.Equal(Encoding.ASCII.GetBytes(expected), beginChunkBytes.ToArray());
        }
        
        [Theory]
        [InlineData("Cookie: \r\n\r\n", 1)]
        [InlineData("Cookie:\r\n\r\n", 1)]
        [InlineData("Cookie:\r\n value\r\n\r\n", 1)]
        [InlineData("Cookie\r\n", 0)]
        [InlineData("Cookie: \r\nConnection: close\r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie: \r\n\r\n", 2)]
        [InlineData("Connection: close\r\nCookie \r\n", 1)]
        [InlineData("Connection:\r\n \r\nCookie \r\n", 1)]
        public void EmptyHeaderValuesCanBeParsed(string rawHeaders, int numHeaders)
        {
            var socketInput = new SocketInput(new MemoryPool2());
            var headerCollection = new FrameRequestHeaders();

            var headerArray = Encoding.ASCII.GetBytes(rawHeaders);
            var inputBuffer = socketInput.IncomingStart(headerArray.Length);
            Buffer.BlockCopy(headerArray, 0, inputBuffer.Data.Array, inputBuffer.Data.Offset, headerArray.Length);
            socketInput.IncomingComplete(headerArray.Length, null);

            var success = Frame.TakeMessageHeaders(socketInput, headerCollection);

            Assert.True(success);
            Assert.Equal(numHeaders, headerCollection.Count());

            // Assert TakeMessageHeaders consumed all the input
            var scan = socketInput.ConsumingStart();
            Assert.True(scan.IsEnd);
        }
    }
}
