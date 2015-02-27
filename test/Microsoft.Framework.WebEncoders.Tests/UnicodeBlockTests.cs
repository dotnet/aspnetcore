// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class UnicodeBlockTests
    {
        [Theory]
        [InlineData(-1, 16)]
        [InlineData(1, 16)]
        [InlineData(0x10000, 16)]
        public void Ctor_FailureCase_FirstCodePoint(int firstCodePoint, int blockSize)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeBlock(firstCodePoint, blockSize));
            Assert.Equal("firstCodePoint", ex.ParamName);
        }

        [Theory]
        [InlineData(0x0100, -1)]
        [InlineData(0x0100, 15)]
        [InlineData(0x0100, 0x10000)]
        public void Ctor_FailureCase_BlockSize(int firstCodePoint, int blockSize)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeBlock(firstCodePoint, blockSize));
            Assert.Equal("blockSize", ex.ParamName);
        }

        [Fact]
        public void Ctor_SuccessCase()
        {
            // Act
            var block = new UnicodeBlock(0x0100, 128); // Latin Extended-A

            // Assert
            Assert.Equal(0x0100, block.FirstCodePoint);
            Assert.Equal(128, block.BlockSize);
        }

        [Theory]
        [InlineData('\u0001', '\u0002')]
        public void FromCharacterRange_FailureCases_FirstChar(char firstChar, char lastChar)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeBlock.FromCharacterRange(firstChar, lastChar));
            Assert.Equal("firstChar", ex.ParamName);
        }

        [Theory]
        [InlineData('\u0100', '\u007F')]
        [InlineData('\u0100', '\u0100')]
        [InlineData('\u0100', '\u010E')]
        public void FromCharacterRange_FailureCases_LastChar(char firstChar, char lastChar)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeBlock.FromCharacterRange(firstChar, lastChar));
            Assert.Equal("lastChar", ex.ParamName);
        }

        [Fact]
        public void FromCharacterRange_SuccessCase()
        {
            // Act
            var block = UnicodeBlock.FromCharacterRange('\u0180', '\u024F'); // Latin Extended-B

            // Assert
            Assert.Equal(0x0180, block.FirstCodePoint);
            Assert.Equal(208, block.BlockSize);
        }

        [Fact]
        public void FromCharacterRange_SuccessCase_All()
        {
            // Act
            var block = UnicodeBlock.FromCharacterRange('\u0000', '\uFFFF');

            // Assert
            Assert.Equal(0, block.FirstCodePoint);
            Assert.Equal(0x10000, block.BlockSize);
        }
    }
}
