// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc;

public class HttpUnauthorizedResultTests
{
    [Fact]
    public void HttpUnauthorizedResult_InitializesStatusCode()
    {
        // Arrange & act
        var result = new UnauthorizedResult();

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }
}
