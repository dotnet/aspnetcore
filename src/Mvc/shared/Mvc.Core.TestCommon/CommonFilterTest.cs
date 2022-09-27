// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class CommonFilterTest
{
    // This is used as a 'common' test method for ActionFilterAttribute and Controller
    public static async Task ActionFilter_Calls_OnActionExecuted(Mock mock)
    {
        // Arrange
        mock.As<IAsyncActionFilter>()
            .Setup(f => f.OnActionExecutionAsync(
                It.IsAny<ActionExecutingContext>(),
                It.IsAny<ActionExecutionDelegate>()))
            .CallBase();

        mock.As<IActionFilter>()
            .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
            .Verifiable();

        mock.As<IActionFilter>()
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Verifiable();

        var context = CreateActionExecutingContext(mock.As<IFilterMetadata>().Object);
        var next = new ActionExecutionDelegate(() => Task.FromResult(CreateActionExecutedContext(context)));

        // Act
        await mock.As<IAsyncActionFilter>().Object.OnActionExecutionAsync(context, next);

        // Assert
        Assert.Null(context.Result);

        mock.As<IActionFilter>()
            .Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());

        mock.As<IActionFilter>()
            .Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());
    }

    // This is used as a 'common' test method for ActionFilterAttribute and Controller
    public static async Task ActionFilter_SettingResult_ShortCircuits(Mock mock)
    {
        // Arrange
        mock.As<IAsyncActionFilter>()
            .Setup(f => f.OnActionExecutionAsync(
                It.IsAny<ActionExecutingContext>(),
                It.IsAny<ActionExecutionDelegate>()))
            .CallBase();

        mock.As<IActionFilter>()
            .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
            .Callback<ActionExecutingContext>(c =>
            {
                mock.ToString();
                c.Result = new NoOpResult();
            });

        mock.As<IActionFilter>()
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Verifiable();

        var context = CreateActionExecutingContext(mock.As<IFilterMetadata>().Object);
        var next = new ActionExecutionDelegate(() => { throw null; }); // This won't run

        // Act
        await mock.As<IAsyncActionFilter>().Object.OnActionExecutionAsync(context, next);

        // Assert
        Assert.IsType<NoOpResult>(context.Result);

        mock.As<IActionFilter>()
            .Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Never());
    }

    // This is used as a 'common' test method for ActionFilterAttribute and ResultFilterAttribute
    public static async Task ResultFilter_Calls_OnResultExecuted(Mock mock)
    {
        // Arrange
        mock.As<IAsyncResultFilter>()
            .Setup(f => f.OnResultExecutionAsync(
                It.IsAny<ResultExecutingContext>(),
                It.IsAny<ResultExecutionDelegate>()))
            .CallBase();

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Verifiable();

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var context = CreateResultExecutingContext(mock.As<IFilterMetadata>().Object);
        var next = new ResultExecutionDelegate(() => Task.FromResult(CreateResultExecutedContext(context)));

        // Act
        await mock.As<IAsyncResultFilter>().Object.OnResultExecutionAsync(context, next);

        // Assert
        Assert.False(context.Cancel);

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
    }

    // This is used as a 'common' test method for ActionFilterAttribute and ResultFilterAttribute
    public static async Task ResultFilter_SettingResult_DoesNotShortCircuit(Mock mock)
    {
        // Arrange
        mock.As<IAsyncResultFilter>()
            .Setup(f => f.OnResultExecutionAsync(
                It.IsAny<ResultExecutingContext>(),
                It.IsAny<ResultExecutionDelegate>()))
            .CallBase();

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c =>
            {
                mock.ToString();
                c.Result = new NoOpResult();
            });

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var context = CreateResultExecutingContext(mock.As<IFilterMetadata>().Object);
        var next = new ResultExecutionDelegate(() => Task.FromResult(CreateResultExecutedContext(context)));

        // Act
        await mock.As<IAsyncResultFilter>().Object.OnResultExecutionAsync(context, next);

        // Assert
        Assert.False(context.Cancel);

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
    }

    // This is used as a 'common' test method for ActionFilterAttribute and ResultFilterAttribute
    public static async Task ResultFilter_SettingCancel_ShortCircuits(Mock mock)
    {
        // Arrange
        mock.As<IAsyncResultFilter>()
            .Setup(f => f.OnResultExecutionAsync(
                It.IsAny<ResultExecutingContext>(),
                It.IsAny<ResultExecutionDelegate>()))
            .CallBase();

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c =>
            {
                mock.ToString();
                c.Cancel = true;
            });

        mock.As<IResultFilter>()
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var context = CreateResultExecutingContext(mock.As<IFilterMetadata>().Object);
        var next = new ResultExecutionDelegate(() => { throw null; }); // This won't run

        // Act
        await mock.As<IAsyncResultFilter>().Object.OnResultExecutionAsync(context, next);

        // Assert
        Assert.True(context.Cancel);

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());

        mock.As<IResultFilter>()
            .Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Never());
    }

    private static ActionExecutingContext CreateActionExecutingContext(IFilterMetadata filter)
    {
        return new ActionExecutingContext(
            CreateActionContext(),
            new IFilterMetadata[] { filter, },
            new Dictionary<string, object>(),
            controller: new object());
    }

    private static ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext context)
    {
        return new ActionExecutedContext(context, context.Filters, context.Controller)
        {
            Result = context.Result,
        };
    }

    private static ResultExecutingContext CreateResultExecutingContext(IFilterMetadata filter)
    {
        return new ResultExecutingContext(
            CreateActionContext(),
            new IFilterMetadata[] { filter, },
            new NoOpResult(),
            controller: new object());
    }

    private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context)
    {
        return new ResultExecutedContext(context, context.Filters, context.Result, context.Controller);
    }

    private static ActionContext CreateActionContext()
    {
        return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
    }

    private sealed class NoOpResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return Task.FromResult(true);
        }
    }
}
