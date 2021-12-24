// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

public class UnauthorizedResultTests
{
    [Fact]
    public void UnauthorizedResult_InitializesStatusCode()
    {
        // Arrange & act
        var result = new UnauthorizedResult();

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }
}
