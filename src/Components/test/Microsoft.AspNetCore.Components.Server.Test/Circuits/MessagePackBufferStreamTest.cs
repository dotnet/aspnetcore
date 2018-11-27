// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Server.Circuits;
using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server
{
    public class MessagePackBufferStreamTest
    {
        [Fact]
        public void NullBuffer_Throws()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
            {
                new MessagePackBufferStream(null, 0);
            });

            Assert.Equal("buffer", ex.ParamName);
        }

        [Fact]
        public void WithWrites_WritesToUnderlyingBuffer()
        {
            // Arrange
            var buffer = new byte[100];
            var offset = 58; // Arbitrary

            // Act/Assert
            using (var stream = new MessagePackBufferStream(buffer, offset))
            {
                stream.Write(new byte[] { 10, 20, 30, 40 }, 1, 2); // Write 2 bytes
                stream.Write(new byte[] { 101 }, 0, 1); // Write another 1 byte
                stream.Close();

                Assert.Equal(20, buffer[offset]);
                Assert.Equal(30, buffer[offset + 1]);
                Assert.Equal(101, buffer[offset + 2]);
            }
        }

        [Fact]
        public void LengthAndPositionAreEquivalent()
        {
            // Arrange
            var buffer = new byte[20];
            var offset = 3;

            // Act/Assert
            using (var stream = new MessagePackBufferStream(buffer, offset))
            {
                stream.Write(new byte[] { 0x01, 0x02 }, 0, 2);
                Assert.Equal(2, stream.Length);
                Assert.Equal(2, stream.Position);
            }
        }

        [Fact]
        public void WithWrites_ExpandsBufferWhenNeeded()
        {
            // Arrange
            var origBuffer = new byte[10];
            var offset = 6;
            origBuffer[0] = 123; // So we can check it was retained during expansion

            // Act/Assert
            using (var stream = new MessagePackBufferStream(origBuffer, offset))
            {
                // We can fit the 6-byte offset plus 3 written bytes
                // into the original 10-byte buffer
                stream.Write(new byte[] { 10, 20, 30 }, 0, 3);
                Assert.Same(origBuffer, stream.Buffer);

                // Trying to add two more exceeds the capacity, so the buffer expands
                stream.Write(new byte[] { 40, 50 }, 0, 2);
                Assert.NotSame(origBuffer, stream.Buffer);
                Assert.True(stream.Buffer.Length > origBuffer.Length);

                // Check the expanded buffer has the expected contents
                stream.Close();
                Assert.Equal(123, stream.Buffer[0]); // Retains other values from original buffer
                Assert.Equal(10, stream.Buffer[offset]);
                Assert.Equal(20, stream.Buffer[offset + 1]);
                Assert.Equal(30, stream.Buffer[offset + 2]);
                Assert.Equal(40, stream.Buffer[offset + 3]);
                Assert.Equal(50, stream.Buffer[offset + 4]);
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
