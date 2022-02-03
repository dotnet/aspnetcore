// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

public class ConflictObjectResultTest
{
    [Fact]
    public void ConflictObjectResult_SetsStatusCodeAndValue()
    {
        // Arrange & Act
        var obj = new object();
        var conflictObjectResult = new ConflictObjectResult(obj);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictObjectResult.StatusCode);
        Assert.Equal(obj, conflictObjectResult.Value);
    }
}
