// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class PageHandlerResultFilterTest
{
    [Fact]
    public async Task OnResultExecutionAsync_ExecutesAsyncFilters()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var model = new Mock<PageModel>();

        var modelAsFilter = model.As<IAsyncResultFilter>();
        modelAsFilter
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var resultExecutingContext = new ResultExecutingContext(
           pageContext,
           Array.Empty<IFilterMetadata>(),
           new PageResult(),
           model.Object);
        var resultExecutedContext = new ResultExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            resultExecutingContext.Result,
            model.Object);
        ResultExecutionDelegate next = () => Task.FromResult(resultExecutedContext);

        var pageHandlerResultFilter = new PageHandlerResultFilter();

        // Act
        await pageHandlerResultFilter.OnResultExecutionAsync(resultExecutingContext, next);

        // Assert
        modelAsFilter.Verify();
    }

    [Fact]
    public async Task OnResultExecutionAsync_ExecutesSyncFilters()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var model = new Mock<object>();

        var modelAsFilter = model.As<IResultFilter>();
        modelAsFilter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Verifiable();

        modelAsFilter
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var resultExecutingContext = new ResultExecutingContext(
           pageContext,
           Array.Empty<IFilterMetadata>(),
           new PageResult(),
           model.Object);
        var resultExecutedContext = new ResultExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            resultExecutingContext.Result,
            model.Object);
        ResultExecutionDelegate next = () => Task.FromResult(resultExecutedContext);

        var pageHandlerResultFilter = new PageHandlerResultFilter();

        // Act
        await pageHandlerResultFilter.OnResultExecutionAsync(resultExecutingContext, next);

        // Assert
        modelAsFilter.Verify();
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_DoesNotInvokeResultExecuted_IfCancelled()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var model = new Mock<object>();

        var modelAsFilter = model.As<IResultFilter>();
        modelAsFilter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback((ResultExecutingContext context) => context.Cancel = true)
            .Verifiable();

        modelAsFilter
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Throws(new Exception("Shouldn't be called"));

        var resultExecutingContext = new ResultExecutingContext(
           pageContext,
           Array.Empty<IFilterMetadata>(),
           new PageResult(),
           model.Object);
        var resultExecutedContext = new ResultExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            resultExecutingContext.Result,
            model.Object);
        ResultExecutionDelegate next = () => Task.FromResult(resultExecutedContext);

        var pageHandlerResultFilter = new PageHandlerResultFilter();

        // Act
        await pageHandlerResultFilter.OnResultExecutionAsync(resultExecutingContext, next);

        // Assert
        modelAsFilter.Verify();
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_InvokesNextDelegateIfHandlerDoesNotImplementFilter()
    {
        // Arrange
        var pageContext = new PageContext(new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new PageActionDescriptor(),
            new ModelStateDictionary()));
        var model = new object();

        var resultExecutingContext = new ResultExecutingContext(
           pageContext,
           Array.Empty<IFilterMetadata>(),
           new PageResult(),
           model);
        var resultExecutedContext = new ResultExecutedContext(
            pageContext,
            Array.Empty<IFilterMetadata>(),
            resultExecutingContext.Result,
            model);
        var invoked = false;
        ResultExecutionDelegate next = () =>
        {
            invoked = true;
            return Task.FromResult(resultExecutedContext);
        };

        var pageHandlerResultFilter = new PageHandlerResultFilter();

        // Act
        await pageHandlerResultFilter.OnResultExecutionAsync(resultExecutingContext, next);

        // Assert
        Assert.True(invoked);
    }
}
