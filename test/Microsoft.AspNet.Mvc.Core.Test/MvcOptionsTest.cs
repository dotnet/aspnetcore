// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class MvcOptionsTest
    {
        [Fact]
        public void MaxValidationError_ThrowsIfValueIsOutOfRange()
        {
            // Arrange
            var options = new MvcOptions();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxModelValidationErrors = -1);
            Assert.Equal("value", ex.ParamName);
        }

        [Fact]
        public void ThrowsWhenMultipleCacheProfilesWithSameNameAreAdded()
        {
            // Arrange
            var options = new MvcOptions();
            options.CacheProfiles.Add("HelloWorld", new CacheProfile { Duration = 10 });

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => options.CacheProfiles.Add("HelloWorld", new CacheProfile { Duration = 5 }));
            Assert.Equal("An item with the same key has already been added.", ex.Message);
        }
    }
}