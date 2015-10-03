// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class AllowedCharsBitmapTests
    {
        [Fact]
        public void Ctor_EmptyByDefault()
        {
            // Act
            var bitmap = AllowedCharsBitmap.CreateNew();

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.False(bitmap.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Allow_Forbid_ZigZag()
        {
            // Arrange
            var bitmap = AllowedCharsBitmap.CreateNew();

            // Act
            // The only chars which are allowed are those whose code points are multiples of 3 or 7
            // who aren't also multiples of 5. Exception: multiples of 35 are allowed.
            for (int i = 0; i <= Char.MaxValue; i += 3)
            {
                bitmap.AllowCharacter((char)i);
            }
            for (int i = 0; i <= Char.MaxValue; i += 5)
            {
                bitmap.ForbidCharacter((char)i);
            }
            for (int i = 0; i <= Char.MaxValue; i += 7)
            {
                bitmap.AllowCharacter((char)i);
            }

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                bool isAllowed = false;
                if (i % 3 == 0) { isAllowed = true; }
                if (i % 5 == 0) { isAllowed = false; }
                if (i % 7 == 0) { isAllowed = true; }
                Assert.Equal(isAllowed, bitmap.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Clear_ForbidsEverything()
        {
            // Arrange
            var bitmap = AllowedCharsBitmap.CreateNew();
            for (int i = 1; i <= Char.MaxValue; i++)
            {
                bitmap.AllowCharacter((char)i);
            }

            // Act
            bitmap.Clear();

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                Assert.False(bitmap.IsCharacterAllowed((char)i));
            }
        }

        [Fact]
        public void Clone_MakesDeepCopy()
        {
            // Arrange
            var originalBitmap = AllowedCharsBitmap.CreateNew();
            originalBitmap.AllowCharacter('x');

            // Act
            var clonedBitmap = originalBitmap.Clone();
            clonedBitmap.AllowCharacter('y');

            // Assert
            Assert.True(originalBitmap.IsCharacterAllowed('x'));
            Assert.False(originalBitmap.IsCharacterAllowed('y'));
            Assert.True(clonedBitmap.IsCharacterAllowed('x'));
            Assert.True(clonedBitmap.IsCharacterAllowed('y'));
        }

        [Fact]
        public void ForbidUndefinedCharacters_RemovesUndefinedChars()
        {
            // Arrange
            // We only allow odd-numbered characters in this test so that
            // we can validate that we properly merged the two bitmaps together
            // rather than simply overwriting the target.
            var bitmap = AllowedCharsBitmap.CreateNew();
            for (int i = 1; i <= Char.MaxValue; i += 2)
            {
                bitmap.AllowCharacter((char)i);
            }

            // Act
            bitmap.ForbidUndefinedCharacters();

            // Assert
            for (int i = 0; i <= Char.MaxValue; i++)
            {
                if (i % 2 == 0)
                {
                    Assert.False(bitmap.IsCharacterAllowed((char)i)); // these chars were never allowed in the original description
                }
                else
                {
                    Assert.Equal(UnicodeHelpers.IsCharacterDefined((char)i), bitmap.IsCharacterAllowed((char)i));
                }
            }
        }
    }
}
