// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
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
    }
}