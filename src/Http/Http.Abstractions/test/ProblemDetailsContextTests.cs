// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class ProblemDetailsContextTests
{
    [Fact]
    public void ProblemDetailsPropertySetter_Should_SetProblemDetails()
    {
        // Arrange
        ProblemDetailsContext context = new() { HttpContext = new DefaultHttpContext() };
        ProblemDetails problemDetails = new();

        // Act
        context.ProblemDetails = problemDetails;

        // Assert
        Assert.Equal(problemDetails, context.ProblemDetails);
    }
}
