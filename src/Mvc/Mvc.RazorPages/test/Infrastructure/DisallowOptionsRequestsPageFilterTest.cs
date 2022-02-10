// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DisallowOptionsRequestsPageFilterTest
{
    [Fact]
    public void OnPageHandlerExecuting_DoesNothing_IfHandlerIsSelected()
    {
        // Arrange
        var context = GetContext(new HandlerMethodDescriptor());
        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_DoesNotOverwriteResult_IfHandlerIsSelected()
    {
        // Arrange
        var expected = new PageResult();
        var context = GetContext(new HandlerMethodDescriptor());
        context.Result = expected;
        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Same(expected, context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_DoesNothing_IfHandlerIsNotSelected_WhenRequestsIsNotOptions()
    {
        // Arrange
        var context = GetContext(handlerMethodDescriptor: null);
        context.HttpContext.Request.Method = "PUT";
        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_DoesNotOverwriteResult_IfHandlerIsNotSelected_WhenRequestsIsNotOptions()
    {
        // Arrange
        var expected = new PageResult();
        var context = GetContext(handlerMethodDescriptor: null);
        context.HttpContext.Request.Method = "DELETE";
        context.Result = expected;

        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Same(expected, context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_DoesNothing_ForOptionsRequestWhenHandlerIsSelected()
    {
        // Arrange
        var context = GetContext(new HandlerMethodDescriptor());
        context.HttpContext.Request.Method = "Options";

        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_DoesNotOverwriteResult_ForOptionsRequestWhenNoHandler()
    {
        // Arrange
        var expected = new NotFoundResult();
        var context = GetContext(new HandlerMethodDescriptor());
        context.Result = expected;
        context.HttpContext.Request.Method = "Options";

        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.Same(expected, context.Result);
    }

    [Fact]
    public void OnPageHandlerExecuting_SetsResult_ForOptionsRequestWhenNoHandlerIsSelected()
    {
        // Arrange
        var context = GetContext(handlerMethodDescriptor: null);
        context.HttpContext.Request.Method = "Options";

        var filter = new HandleOptionsRequestsPageFilter();

        // Act
        filter.OnPageHandlerExecuting(context);

        // Assert
        Assert.IsType<OkResult>(context.Result);
    }

    private static PageHandlerExecutingContext GetContext(HandlerMethodDescriptor handlerMethodDescriptor)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new PageActionDescriptor());
        var pageContext = new PageContext(actionContext);
        return new PageHandlerExecutingContext(pageContext, Array.Empty<IFilterMetadata>(), handlerMethodDescriptor, new Dictionary<string, object>(), new object());
    }
}
