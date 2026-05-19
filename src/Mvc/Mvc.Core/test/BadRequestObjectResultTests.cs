// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

public class BadRequestObjectResultTests
{
    [Fact]
    public void BadRequestObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var badRequestObjectResult = new BadRequestObjectResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
        Assert.Equal(obj, badRequestObjectResult.Value);
    }

    [Fact]
    public void BadRequestObjectResult_ModelState_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var badRequestObjectResult = new BadRequestObjectResult(new ModelStateDictionary());

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestObjectResult.StatusCode);
        var errors = Assert.IsType<SerializableError>(badRequestObjectResult.Value);
        Assert.Empty(errors);
    }
}
