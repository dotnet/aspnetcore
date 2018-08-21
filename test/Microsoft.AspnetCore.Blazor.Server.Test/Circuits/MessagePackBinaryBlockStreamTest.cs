// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MessagePack;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Server
{
    public class MessagePackBinaryBlockStreamTest
    {
        [Fact]
        public void NullBuffer_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new MessagePackBinaryBlockStream(null, 0);
            });

            Assert.Equal("buffer", ex.ParamName);
        }

        [Fact]
        public void WithNoWrites_JustOutputsHeader()
        {
            // Arrange
            var buffer = new byte[100];
            var offset = 58; // Arbitrary

            // Act
            new MessagePackBinaryBlockStream(buffer, offset).Dispose();

            // Assert
            Assert.Equal(MessagePackCode.Bin32, buffer[offset]);
            Assert.Equal(0, ReadBigEndianInt32(buffer, offset + 1));
        }

        [Fact]
        public void WithWrites_WritesToUnderlyingBuffer()
        {
            // Arrange
            var buffer = new byte[100];
            var offset = 58; // Arbitrary

            // Act/Assert
            using (var stream = new MessagePackBinaryBlockStream(buffer, offset))
            {
                stream.Write(new byte[] { 10, 20, 30, 40 }, 1, 2); // Write 2 bytes
                stream.Write(new byte[] { 101 }, 0, 1); // Write another 1 byte
                stream.Close();

                Assert.Equal(MessagePackCode.Bin32, buffer[offset]);
                Assert.Equal(3, ReadBigEndianInt32(buffer, offset + 1));
                Assert.Equal(20, buffer[offset + 5]);
                Assert.Equal(30, buffer[offset + 6]);
                Assert.Equal(101, buffer[offset + 7]);
            }
        }

        [Fact]
        public void LengthIncludesHeaderButPositionDoesNot()
        {
            // Arrange
            var buffer = new byte[20];
            var offset = 3;

            // Act/Assert
            using (var stream = new MessagePackBinaryBlockStream(buffer, offset))
            {
                stream.Write(new byte[] { 0x01, 0x02 }, 0, 2);
                Assert.Equal(7, stream.Length);
                Assert.Equal(2, stream.Position);
            }
        }

        [Fact]
        public void WithWrites_ExpandsBufferWhenNeeded()
        {
            // Arrange
            var origBuffer = new byte[15];
            var offset = 6;
            origBuffer[0] = 123; // So we can check it was retained during expansion

            // Act/Assert
            using (var stream = new MessagePackBinaryBlockStream(origBuffer, offset))
            {
                // We can fit the 6-byte offset plus 5-byte header plus 3 written bytes
                // into the original 15-byte buffer
                stream.Write(new byte[] { 10, 20, 30 }, 0, 3);
                Assert.Same(origBuffer, stream.Buffer);

                // Trying to add two more exceeds the capacity, so the buffer expands
                stream.Write(new byte[] { 40, 50 }, 0, 2);
                Assert.NotSame(origBuffer, stream.Buffer);
                Assert.True(stream.Buffer.Length > origBuffer.Length);

                // Check the expanded buffer has the expected contents
                stream.Close();
                Assert.Equal(123, stream.Buffer[0]); // Retains other values from original buffer
                Assert.Equal(MessagePackCode.Bin32, stream.Buffer[offset]);
                Assert.Equal(5, ReadBigEndianInt32(stream.Buffer, offset + 1));
                Assert.Equal(10, stream.Buffer[offset + 5]);
                Assert.Equal(20, stream.Buffer[offset + 6]);
                Assert.Equal(30, stream.Buffer[offset + 7]);
                Assert.Equal(40, stream.Buffer[offset + 8]);
                Assert.Equal(50, stream.Buffer[offset + 9]);
            }
        }

        int ReadBigEndianInt32(byte[] buffer, int startOffset)
        {
            return (buffer[startOffset] << 24)
                + (buffer[startOffset + 1] << 16)
                + (buffer[startOffset + 2] << 8)
                + (buffer[startOffset + 3]);
        }
    }
}
