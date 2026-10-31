// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class UnsupportedContentTypeFilterTest
{
    [Fact]
    public void OnActionExecuting_ChangesActionResult_IfUnsupportedContentTypeExceptionIsFoundOnModelState()
    {
        // Arrange
        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());

        var modelMetadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(int));

        context.ModelState.AddModelError(
            "person.body",
            new UnsupportedContentTypeException("error"),
            modelMetadata);

        var filter = new UnsupportedContentTypeFilter();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.NotNull(context.Result);
        var status = Assert.IsType<UnsupportedMediaTypeResult>(context.Result);
    }

    [Fact]
    public void OnActionExecuting_DoesNotChangeActionResult_IfOtherErrorsAreFoundOnModelState()
    {
        // Arrange
        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());

        context.ModelState.AddModelError("person.body", "Some error");

        var filter = new UnsupportedContentTypeFilter();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_DoesNotChangeActionResult_IfModelStateIsValid()
    {
        // Arrange
        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());

        var filter = new UnsupportedContentTypeFilter();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_DoesNotChangeActionResult_IfOtherExceptionsAreFoundOnModelState()
    {
        // Arrange
        var context = new ActionExecutingContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(),
                ActionDescriptor = new ActionDescriptor()
            },
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());

        var modelMetadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(int));

        context.ModelState.AddModelError(
            "person.body",
            new Exception("error"),
            modelMetadata);

        var filter = new UnsupportedContentTypeFilter();

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }
}
