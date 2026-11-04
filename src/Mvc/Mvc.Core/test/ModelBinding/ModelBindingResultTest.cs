// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test;

public class ModelBindingResultTest
{
    [Fact]
    public void Success_SetsProperties()
    {
        // Arrange
        var model = "some model";

        // Act
        var result = ModelBindingResult.Success(model);

        // Assert
        Assert.True(result.IsModelSet);
        Assert.Same(model, result.Model);
    }

    [Fact]
    public void Failed_SetsProperties()
    {
        // Arrange & Act
        var result = ModelBindingResult.Failed();

        // Assert
        Assert.False(result.IsModelSet);
        Assert.Null(result.Model);
    }
}
