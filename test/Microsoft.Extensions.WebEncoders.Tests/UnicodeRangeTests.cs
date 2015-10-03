// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class UnicodeRangeTests
    {
        [Theory]
        [InlineData(-1, 16)]
        [InlineData(0x10000, 16)]
        public void Ctor_FailureCase_FirstCodePoint(int firstCodePoint, int rangeSize)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(firstCodePoint, rangeSize));
            Assert.Equal("firstCodePoint", ex.ParamName);
        }

        [Theory]
        [InlineData(0x0100, -1)]
        [InlineData(0x0100, 0x10000)]
        public void Ctor_FailureCase_RangeSize(int firstCodePoint, int rangeSize)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new UnicodeRange(firstCodePoint, rangeSize));
            Assert.Equal("rangeSize", ex.ParamName);
        }

        [Fact]
        public void Ctor_SuccessCase()
        {
            // Act
            var range = new UnicodeRange(0x0100, 128); // Latin Extended-A

            // Assert
            Assert.Equal(0x0100, range.FirstCodePoint);
            Assert.Equal(128, range.RangeSize);
        }

        [Fact]
        public void FromSpan_FailureCase()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => UnicodeRange.FromSpan('\u0020', '\u0010'));
            Assert.Equal("lastChar", ex.ParamName);
        }

        [Fact]
        public void FromSpan_SuccessCase()
        {
            // Act
            var range = UnicodeRange.FromSpan('\u0180', '\u024F'); // Latin Extended-B

            // Assert
            Assert.Equal(0x0180, range.FirstCodePoint);
            Assert.Equal(208, range.RangeSize);
        }

        [Fact]
        public void FromSpan_SuccessCase_All()
        {
            // Act
            var range = UnicodeRange.FromSpan('\u0000', '\uFFFF');

            // Assert
            Assert.Equal(0, range.FirstCodePoint);
            Assert.Equal(0x10000, range.RangeSize);
        }
    }
}
