// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ClientErrorResultFilterTest
{
    private static readonly IActionResult Result = new EmptyResult();

    [Fact]
    public void OnResultExecuting_DoesNothing_IfActionIsNotClientErrorActionResult()
    {
        // Arrange
        var actionResult = new NotFoundObjectResult(new object());
        var context = GetContext(actionResult);
        var filter = GetFilter();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Same(actionResult, context.Result);
    }

    [Fact]
    public void OnResultExecuting_DoesNothing_IfTransformedValueIsNull()
    {
        // Arrange
        var actionResult = new NotFoundResult();
        var context = GetContext(actionResult);
        var factory = new Mock<IClientErrorFactory>();
        factory
            .Setup(f => f.GetClientError(It.IsAny<ActionContext>(), It.IsAny<IClientErrorActionResult>()))
            .Returns((IActionResult)null)
            .Verifiable();

        var filter = new ClientErrorResultFilter(factory.Object, NullLogger<ClientErrorResultFilter>.Instance);

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Same(actionResult, context.Result);
        factory.Verify();
    }

    [Fact]
    public void OnResultExecuting_TransformsClientErrors()
    {
        // Arrange
        var actionResult = new NotFoundResult();
        var context = GetContext(actionResult);
        var filter = GetFilter();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Same(Result, context.Result);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(409)]
    [InlineData(503)]
    public void OnResultExecuting_Transforms4XXStatusCodeResult(int statusCode)
    {
        // Arrange
        var actionResult = new StatusCodeResult(statusCode);
        var context = GetContext(actionResult);
        var filter = GetFilter();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Same(Result, context.Result);
    }

    [Theory]
    [InlineData(201)]
    [InlineData(302)]
    [InlineData(399)]
    public void OnResultExecuting_DoesNotTransformStatusCodesLessThan400(int statusCode)
    {
        // Arrange
        var actionResult = new StatusCodeResult(statusCode);
        var context = GetContext(actionResult);
        var filter = GetFilter();

        // Act
        filter.OnResultExecuting(context);

        // Assert
        Assert.Same(actionResult, context.Result);
    }

    private static ClientErrorResultFilter GetFilter()
    {
        var factory = Mock.Of<IClientErrorFactory>(
            f => f.GetClientError(It.IsAny<ActionContext>(), It.IsAny<IClientErrorActionResult>()) == Result);

        return new ClientErrorResultFilter(factory, NullLogger<ClientErrorResultFilter>.Instance);
    }

    private static ResultExecutingContext GetContext(IActionResult actionResult)
    {
        return new ResultExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            Array.Empty<IFilterMetadata>(),
            actionResult,
            new object());
    }
}
