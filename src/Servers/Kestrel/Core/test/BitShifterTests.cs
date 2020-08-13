// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class BitShifterTests
    {
        [Fact]
        public void WriteUInt31BigEndian_PreservesHighestBit()
        {
            // Arrange
            Span<byte> dirtySpan = new byte[] { 0xff, 0xff, 0xff, 0xff };

            // Act
            Bitshifter.WriteUInt31BigEndian(dirtySpan, 1);

            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x01 }, dirtySpan.ToArray());
        }

        [Fact]
        public void WriteUInt31BigEndian_True_PreservesHighestBit()
        {
            // Arrange
            Span<byte> dirtySpan = new byte[] { 0xff, 0xff, 0xff, 0xff };

            // Act
            Bitshifter.WriteUInt31BigEndian(dirtySpan, 1, true);

            Assert.Equal(new byte[] { 0x80, 0x00, 0x00, 0x01 }, dirtySpan.ToArray());
        }

        [Fact]
        public void WriteUInt31BigEndian_False_OverwritesHighestBit()
        {
            // Arrange
            Span<byte> dirtySpan = new byte[] { 0xff, 0xff, 0xff, 0xff };

            // Act
            Bitshifter.WriteUInt31BigEndian(dirtySpan, 1, false);

            Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, dirtySpan.ToArray());
        }
    }
}
