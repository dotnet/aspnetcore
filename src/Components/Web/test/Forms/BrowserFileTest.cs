// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class BrowserFileTest
    {
        [Fact]
        public void SetSize_ThrowsIfSizeIsNegative()
        {
            // Arrange
            var file = new BrowserFile();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => file.Size = -7);
        }

        [Fact]
        public void OpenReadStream_ThrowsIfFileSizeIsLargerThanAllowedSize()
        {
            // Arrange
            var file = new BrowserFile { Size = 100 };

            // Act & Assert
            var ex = Assert.Throws<IOException>(() => file.OpenReadStream(80));
            Assert.Equal("Supplied file with size 100 bytes exceeds the maximum of 80 bytes.", ex.Message);
        }
    }
}
