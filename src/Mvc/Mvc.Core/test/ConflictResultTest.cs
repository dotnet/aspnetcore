// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc;

public class ConflictResultTest
{
    [Fact]
    public void ConflictResult_InitializesStatusCode()
    {
        // Arrange & act
        var conflictResult = new ConflictResult();

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, conflictResult.StatusCode);
    }
}
