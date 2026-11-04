// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

public class UnprocessableEntityObjectResultTests
{
    [Fact]
    public void UnprocessableEntityObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var result = new UnprocessableEntityObjectResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        Assert.Equal(obj, result.Value);
    }

    [Fact]
    public void UnprocessableEntityObjectResult_ModelState_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var result = new UnprocessableEntityObjectResult(new ModelStateDictionary());

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        var errors = Assert.IsType<SerializableError>(result.Value);
        Assert.Empty(errors);
    }
}
