// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

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
