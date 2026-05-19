// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ModelStateInvalidFilterTest
{
    [Fact]
    public void OnActionExecuting_NoOpsIfResultIsAlreadySet()
    {
        // Arrange
        var options = new ApiBehaviorOptions
        {
            InvalidModelStateResponseFactory = _ => new BadRequestResult(),
        };
        var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
        var context = GetActionExecutingContext();
        var expected = new OkResult();
        context.Result = expected;

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Same(expected, context.Result);
    }

    [Fact]
    public void OnActionExecuting_NoOpsIfModelStateIsValid()
    {
        // Arrange
        var options = new ApiBehaviorOptions
        {
            InvalidModelStateResponseFactory = _ => new BadRequestResult(),
        };
        var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
        var context = GetActionExecutingContext();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_InvokesResponseFactoryIfModelStateIsInvalid()
    {
        // Arrange
        var expected = new BadRequestResult();
        var options = new ApiBehaviorOptions
        {
            InvalidModelStateResponseFactory = _ => expected,
        };
        var filter = new ModelStateInvalidFilter(options, NullLogger.Instance);
        var context = GetActionExecutingContext();
        context.ModelState.AddModelError("some-key", "some-error");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Same(expected, context.Result);
    }

    private static ActionExecutingContext GetActionExecutingContext()
    {
        return new ActionExecutingContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            Array.Empty<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
    }
}
