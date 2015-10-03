// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class CodePointFilterTests
    {
        [Fact]
        public void Ctor_Parameterless_CreatesEmptyFilter()
        {
            // Act
            var filter = new CodePointFilter();

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Ctor_OtherCodePointFilterAsInterface()
        {
            // Arrange
            var originalFilter = new OddCodePointFilter();

            // Act
            var newFilter = new CodePointFilter(originalFilter);

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.Equal((i % 2) == 1, newFilter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Ctor_OtherCodePointFilterAsConcreteType_Clones()
        {
            // Arrange
            var originalFilter = new CodePointFilter().AllowChar('x');

            // Act
            var newFilter = new CodePointFilter(originalFilter).AllowChar('y');

            // Assert
            Assert.True(originalFilter.IsCharacterAllowed('x'));
            Assert.False(originalFilter.IsCharacterAllowed('y'));
            Assert.True(newFilter.IsCharacterAllowed('x'));
            Assert.True(newFilter.IsCharacterAllowed('y'));
        }

        [Fact]
        public void Ctor_UnicodeRanges()
        {
            // Act
            var filter = new CodePointFilter(UnicodeRanges.LatinExtendedA, UnicodeRanges.LatinExtendedC);

            // Assert
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i < 0x2C60; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C60; i <= 0x2C7F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C80; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowChar()
        {
            // Arrange
            var filter = new CodePointFilter();

            // Act
            var retVal = filter.AllowChar('\u0100');

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
        }

        [Fact]
        public void AllowChars_Array()
        {
            // Arrange
            var filter = new CodePointFilter();

            // Act
            var retVal = filter.AllowChars('\u0100', '\u0102');

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
            Assert.True(filter.IsCharacterAllowed('\u0102'));
            Assert.False(filter.IsCharacterAllowed('\u0103'));
        }

        [Fact]
        public void AllowChars_String()
        {
            // Arrange
            var filter = new CodePointFilter();

            // Act
            var retVal = filter.AllowChars("\u0100\u0102");

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('\u0100'));
            Assert.False(filter.IsCharacterAllowed('\u0101'));
            Assert.True(filter.IsCharacterAllowed('\u0102'));
            Assert.False(filter.IsCharacterAllowed('\u0103'));
        }

        [Fact]
        public void AllowFilter()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeRanges.BasicLatin);

            // Act
            var retVal = filter.AllowFilter(new OddCodePointFilter());

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i <= 0x007F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0080; i <= Char.MaxValue; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowRange()
        {
            // Arrange
            var filter = new CodePointFilter();

            // Act
            var retVal = filter.AllowRange(UnicodeRanges.LatinExtendedA);

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void AllowRanges()
        {
            // Arrange
            var filter = new CodePointFilter();

            // Act
            var retVal = filter.AllowRanges(UnicodeRanges.LatinExtendedA, UnicodeRanges.LatinExtendedC);

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i < 0x0100; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0100; i <= 0x017F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0180; i < 0x2C60; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C60; i <= 0x2C7F; i++)
            {
                Assert.True(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x2C80; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Clear()
        {
            // Arrange
            var filter = new CodePointFilter();
            for (int i = 1; i <= Char.MaxValue; i++)
            {
                filter.AllowChar((char)i);
            }

            // Act
            var retVal = filter.Clear();

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void ForbidChar()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeRanges.BasicLatin);

            // Act
            var retVal = filter.ForbidChar('x');

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.True(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidChars_Array()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeRanges.BasicLatin);

            // Act
            var retVal = filter.ForbidChars('x', 'z');

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.False(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidChars_String()
        {
            // Arrange
            var filter = new CodePointFilter(UnicodeRanges.BasicLatin);

            // Act
            var retVal = filter.ForbidChars("xz");

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            Assert.True(filter.IsCharacterAllowed('w'));
            Assert.False(filter.IsCharacterAllowed('x'));
            Assert.True(filter.IsCharacterAllowed('y'));
            Assert.False(filter.IsCharacterAllowed('z'));
        }

        [Fact]
        public void ForbidRange()
        {
            // Arrange
            var filter = new CodePointFilter(new OddCodePointFilter());

            // Act
            var retVal = filter.ForbidRange(UnicodeRanges.Specials);

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i <= 0xFFEF; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0xFFF0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void ForbidRanges()
        {
            // Arrange
            var filter = new CodePointFilter(new OddCodePointFilter());

            // Act
            var retVal = filter.ForbidRanges(UnicodeRanges.BasicLatin, UnicodeRanges.Specials);

            // Assert
            Assert.Same(filter, retVal); // returns 'this' instance
            for (int i = 0; i <= 0x007F; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0x0080; i <= 0xFFEF; i++)
            {
                Assert.Equal((i % 2) == 1, filter.IsCharacterAllowed((char)i));
            }
            for (int i = 0xFFF0; i <= Char.MaxValue; i++)
            {
                Assert.False(filter.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void GetAllowedCodePoints()
        {
            // Arrange
            var expected = Enumerable.Range(UnicodeRanges.BasicLatin.FirstCodePoint, UnicodeRanges.BasicLatin.RangeSize)
                .Concat(Enumerable.Range(UnicodeRanges.Specials.FirstCodePoint, UnicodeRanges.Specials.RangeSize))
                .Except(new int[] { 'x' })
                .OrderBy(i => i)
                .ToArray();

            var filter = new CodePointFilter(UnicodeRanges.BasicLatin, UnicodeRanges.Specials);
            filter.ForbidChar('x');

            // Act
            var retVal = filter.GetAllowedCodePoints().OrderBy(i => i).ToArray();

            // Assert
            Assert.Equal<int>(expected, retVal);
        }

        // a code point filter which allows only odd code points through
        private sealed class OddCodePointFilter : ICodePointFilter
        {
            public IEnumerable<int> GetAllowedCodePoints()
            {
                for (int i = 1; i <= Char.MaxValue; i += 2)
                {
                    yield return i;
                }
            }
        }
    }
}
