// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.InternalTesting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc;

public abstract class CommonResourceInvokerTest
{
    protected static readonly TestResult Result = new TestResult();

    // Intentionally choosing an uncommon exception type.
    protected static readonly Exception Exception = new DivideByZeroException();

    protected IActionInvoker CreateInvoker(
        IFilterMetadata filter,
        Exception exception = null,
        IActionResult result = null,
        IList<IValueProviderFactory> valueProviderFactories = null)
    {
        return CreateInvoker(new IFilterMetadata[] { filter }, exception, result, valueProviderFactories);
    }

    protected abstract IActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        Exception exception = null,
        IActionResult result = null,
        IList<IValueProviderFactory> valueProviderFactories = null);

    [Fact]
    public async Task InvokeAction_DoesNotInvokeExceptionFilter_WhenActionDoesNotThrow()
    {
        // Arrange
        var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Verifiable();

        var invoker = CreateInvoker(filter.Object, exception: null);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Never());
    }

    [Fact]
    public async Task InvokeAction_DoesNotAsyncInvokeExceptionFilter_WhenActionDoesNotThrow()
    {
        // Arrange
        var filter = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var invoker = CreateInvoker(filter.Object, exception: null);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Never());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_WhenActionThrows()
    {
        // Arrange
        Exception exception = null;
        IActionResult resultFromAction = null;
        var expected = new Mock<IActionResult>(MockBehavior.Strict);
        expected
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Verifiable();
        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                exception = context.Exception;
                resultFromAction = context.Result;

                // Handle the exception
                context.Result = expected.Object;
            })
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());

        Assert.Same(Exception, exception);
        Assert.Null(resultFromAction);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_WhenActionThrows()
    {
        // Arrange
        Exception exception = null;
        IActionResult resultFromAction = null;
        var expected = new Mock<IActionResult>(MockBehavior.Strict);
        expected
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();
        var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                exception = context.Exception;
                resultFromAction = context.Result;

                // Handle the exception
                context.Result = expected.Object;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        filter2.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Once());

        Assert.Same(Exception, exception);
        Assert.Null(resultFromAction);
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_ShortCircuit_ExceptionNull_WithoutResult()
    {
        // Arrange
        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                filter2.ToString();
                context.Exception = null;
            })
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnException(It.IsAny<ExceptionContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_ShortCircuit_ExceptionNull_WithResult()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.Result = result.Object;
                context.Exception = null;
            })
            .Verifiable();

        // Result filters are never used when an exception bubbles up to exception filters.
        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnException(It.IsAny<ExceptionContext>()),
            Times.Once());

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_ShortCircuit_ExceptionHandled_WithoutResult()
    {
        // Arrange
        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.ExceptionHandled = true;
            })
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnException(It.IsAny<ExceptionContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_ShortCircuit_ExceptionHandled_WithResult()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.Result = result.Object;
                context.ExceptionHandled = true;
            })
            .Verifiable();

        // Result filters are never used when an exception bubbles up to exception filters.
        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnException(It.IsAny<ExceptionContext>()),
            Times.Once());

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionNull_WithoutResult()
    {
        // Arrange
        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);

        filter2
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.Exception = null;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var filterMetadata = new IFilterMetadata[] { filter1.Object, filter2.Object };
        var invoker = CreateInvoker(filterMetadata, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionNull_WithResult()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);

        filter2
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.Exception = null;
                context.Result = result.Object;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        // Result filters are never used when an exception bubbles up to exception filters.
        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Once());

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionHandled_WithoutResult()
    {
        // Arrange
        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

        var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.ExceptionHandled = true;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionHandled_WithResult()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);

        filter2
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.ExceptionHandled = true;
                context.Result = result.Object;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        // Result filters are never used when an exception bubbles up to exception filters.
        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter2.Verify(
            f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
            Times.Once());

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncExceptionFilter_SettingResultDoesNotShortCircuit()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns<ActionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                context.Result = result.Object;
            })
            .Returns<ExceptionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => { }) // Does nothing, we just want to verify that it was called.
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter1.Verify(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()), Times.Once());
        filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_UnhandledExceptionIsThrown()
    {
        // Arrange
        var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Verifiable();

        var invoker = CreateInvoker(filter.Object, exception: Exception);

        // Act
        await Assert.ThrowsAsync(Exception.GetType(), invoker.InvokeAsync);

        // Assert
        filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_ResultIsExecuted_WithoutResultFilters()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns<ActionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => c.Result = result.Object)
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { filter.Object, resultFilter.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesExceptionFilter_SettingResultDoesNotShortCircuit()
    {
        // Arrange
        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns<ActionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => c.Result = result.Object)
            .Verifiable();

        var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => { }) // Does nothing, we just want to verify that it was called.
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { filter1.Object, filter2.Object, resultFilter.Object },
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter1.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
        filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_WithExceptionFilterInTheStack_InvokesResultFilter()
    {
        // Arrange
        var exceptionFilter = new Mock<IExceptionFilter>();
        var resultFilter = new Mock<IResultFilter>();

        var invoker = CreateInvoker(
            new IFilterMetadata[] { exceptionFilter.Object, resultFilter.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        exceptionFilter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Never());
        resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAuthorizationFilter()
    {
        // Arrange
        var filter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>())).Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncAuthorizationFilter()
    {
        // Arrange
        var filter = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()))
            .Returns<AuthorizationFilterContext>(context => Task.FromResult(true))
            .Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(
            f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAuthorizationFilter_ShortCircuit()
    {
        // Arrange
        var challenge = new Mock<IActionResult>(MockBehavior.Strict);
        challenge
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>(c => Task.FromResult(true))
            .Verifiable();

        var filter2 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>(c => c.Result = challenge.Object)
            .Verifiable();

        var filter3 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object, filter3.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        challenge.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        filter1.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncAuthorizationFilter_ShortCircuit()
    {
        // Arrange
        var challenge = new Mock<IActionResult>(MockBehavior.Strict);
        challenge
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        var filter1 = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
        filter1
            .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()))
            .Returns<AuthorizationFilterContext>((context) =>
            {
                return Task.FromResult(true);
            })
            .Verifiable();

        var filter2 = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()))
            .Returns<AuthorizationFilterContext>((context) =>
            {
                context.Result = challenge.Object;
                return Task.FromResult(true);
            });

        var filter3 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        challenge.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        filter1.Verify(
            f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_ExceptionInAuthorizationFilter_CannotBeHandledByOtherFilters()
    {
        // Arrange
        var expected = new InvalidCastException();

        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        exceptionFilter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(context =>
            {
                // Mark as handled
                context.Result = new EmptyResult();
            })
            .Verifiable();

        var authorizationFilter1 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter1
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>(c => { throw expected; })
            .Verifiable();

        // None of these filters should run
        var authorizationFilter2 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        var resourceFilter = new Mock<IResourceFilter>(MockBehavior.Strict);
        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[]
        {
                exceptionFilter.Object,
                authorizationFilter1.Object,
                authorizationFilter2.Object,
                resourceFilter.Object,
                resultFilter.Object,
        });

        // Act
        var thrown = await Assert.ThrowsAsync<InvalidCastException>(invoker.InvokeAsync);

        // Assert
        Assert.Same(expected, thrown);
        exceptionFilter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Never());
        authorizationFilter1.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAuthorizationFilter_ChallengeNotSeenByResultFilters()
    {
        // Arrange
        var challenge = new Mock<IActionResult>(MockBehavior.Strict);
        challenge
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns<ActionContext>((context) => Task.FromResult(true))
            .Verifiable();

        var authorizationFilter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>(c => c.Result = challenge.Object)
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resultFilter.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        authorizationFilter.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
        challenge.Verify(c => c.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesResultFilter()
    {
        // Arrange
        var filter = new Mock<IResultFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
        filter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter()
    {
        // Arrange
        var filter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>(async (context, next) => await next())
            .Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesResultFilter_ShortCircuit_WithCancel()
    {
        // Arrange
        ResultExecutedContext context = null;

        var filter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        filter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
        filter1
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => context = c)
            .Verifiable();

        var filter2 = new Mock<IResultFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c =>
            {
                filter2.ToString();
                c.Cancel = true;
            })
            .Verifiable();

        var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

        filter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter2.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Never());

        Assert.True(context.Canceled);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter_ShortCircuit_WithCancel()
    {
        // Arrange
        ResultExecutedContext context = null;

        var filter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        filter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
        filter1
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => context = c)
            .Verifiable();

        var filter2 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) =>
            {
                // Not calling next here
                c.Cancel = true;
                return Task.FromResult(true);
            })
            .Verifiable();

        var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object }, result: Result);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

        filter2.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());

        Assert.True(context.Canceled);
        Assert.Same(Result, context.Result);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter_ShortCircuit_WithoutCancel()
    {
        // Arrange
        ResultExecutedContext context = null;

        var filter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        filter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
        filter1
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => context = c)
            .Verifiable();

        var filter2 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        filter2
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) =>
            {
                // Not calling next here
                return Task.FromResult(true);
            })
            .Verifiable();

        var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object }, result: Result);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

        filter2.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());

        Assert.True(context.Canceled);
        Assert.Same(Result, context.Result);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter_ShortCircuit_WithoutCancel_CallNext()
    {
        // Arrange
        var filter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>(async (c, next) =>
            {
                // Not calling next here
                c.Cancel = true;
                await next();
            })
            .Verifiable();

        var message =
            "If an IAsyncResultFilter cancels execution by setting the Cancel property of " +
            "ResultExecutingContext to 'true', then it cannot call the next filter by invoking " +
            "ResultExecutionDelegate.";

        var invoker = CreateInvoker(filter.Object);

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            invoker.InvokeAsync,
            message);
    }

    [Fact]
    public async Task InvokeAction_InvokesResultFilter_ExceptionGoesUnhandled()
    {
        // Arrange
        var exception = new DataMisalignedException();

        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Throws(exception)
            .Verifiable();

        var filter = new Mock<IResultFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c => c.Result = result.Object)
            .Verifiable();

        filter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await Assert.ThrowsAsync(exception.GetType(), invoker.InvokeAsync);

        // Assert
        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());

        filter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesResultFilter_WithExceptionThrownByResult()
    {
        // Arrange
        ResultExecutedContext context = null;
        var exception = new DataMisalignedException();

        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Throws(exception)
            .Verifiable();

        var filter = new Mock<IResultFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c => c.Result = result.Object)
            .Verifiable();

        filter
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c =>
            {
                context = c;

                // Handle the exception
                Assert.False(c.ExceptionHandled);
                c.ExceptionHandled = true;
            })
            .Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(exception, context.Exception);

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());

        filter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        filter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter_WithExceptionThrownByResult()
    {
        // Arrange
        ResultExecutedContext context = null;
        var exception = new DataMisalignedException();

        var result = new Mock<IActionResult>(MockBehavior.Strict);
        result
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Throws(exception)
            .Verifiable();

        var filter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        filter
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>(async (c, next) =>
            {
                c.Result = result.Object;

                context = await next();

                // Handle the exception
                Assert.False(context.ExceptionHandled);
                context.ExceptionHandled = true;
            })
            .Verifiable();

        var invoker = CreateInvoker(filter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(exception, context.Exception);

        result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());

        filter.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesResultFilter_WithExceptionThrownByResultFilter()
    {
        // Arrange
        ResultExecutedContext context = null;
        var exception = new DataMisalignedException();

        var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
        resultFilter1
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c =>
            {
                context = c;

                // Handle the exception
                Assert.False(c.ExceptionHandled);
                c.ExceptionHandled = true;
            })
            .Verifiable();

        var resultFilter2 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter2
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Throws(exception)
            .Verifiable();

        var resultFilter3 = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { resultFilter1.Object, resultFilter2.Object, resultFilter3.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(exception, context.Exception);

        resultFilter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        resultFilter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

        resultFilter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResultFilter_WithExceptionThrownByResultFilter()
    {
        // Arrange
        ResultExecutedContext context = null;
        var exception = new DataMisalignedException();

        var resultFilter1 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        resultFilter1
            .Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>(async (c, next) =>
            {
                context = await next();

                // Handle the exception
                Assert.False(context.ExceptionHandled);
                context.ExceptionHandled = true;
            })
            .Verifiable();

        var resultFilter2 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter2
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Throws(exception)
            .Verifiable();

        var resultFilter3 = new Mock<IResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(new IFilterMetadata[] { resultFilter1.Object, resultFilter2.Object, resultFilter3.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(exception, context.Exception);

        resultFilter1.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());

        resultFilter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter()
    {
        // Arrange
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                await next();
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesResourceFilter()
    {
        // Arrange
        var resourceFilter = new Mock<IResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
            .Verifiable();
        resourceFilter
            .Setup(f => f.OnResourceExecuted(It.IsAny<ResourceExecutedContext>()))
            .Verifiable();

        var invoker = CreateInvoker(resourceFilter.Object);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(
            f => f.OnResourceExecuted(It.IsAny<ResourceExecutedContext>()),
            Times.Once());
        resourceFilter.Verify(
            f => f.OnResourceExecuted(It.IsAny<ResourceExecutedContext>()),
            Times.Once());

    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_WithActionResult_FromAction()
    {
        // Arrange
        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var invoker = CreateInvoker(resourceFilter.Object, result: Result);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(Result, context.Result);

        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_WithActionResult_FromExceptionFilter()
    {
        // Arrange
        var expected = Mock.Of<IActionResult>();

        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        exceptionFilter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>((c) =>
            {
                c.Result = expected;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, exceptionFilter.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(expected, context.Result);
        Assert.Null(context.Exception);
        Assert.Null(context.ExceptionDispatchInfo);

        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_WithActionResult_FromResultFilter()
    {
        // Arrange
        var expected = Mock.Of<IActionResult>();

        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Loose);
        resultFilter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>((c) =>
            {
                c.Result = expected;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, resultFilter.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(expected, context.Result);

        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_HandleException_FromAction()
    {
        // Arrange
        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
                context.ExceptionHandled = true;
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(Exception, context.Exception);
        Assert.Same(Exception, context.ExceptionDispatchInfo.SourceException);

        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_HandlesException_FromResultFilter()
    {
        // Arrange
        var expected = new DataMisalignedException();

        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
                context.ExceptionHandled = true;
            })
            .Verifiable();

        var resultFilter = new Mock<IResultFilter>(MockBehavior.Loose);
        resultFilter
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>((c) =>
            {
                throw expected;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, resultFilter.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Same(expected, context.Exception);
        Assert.Same(expected, context.ExceptionDispatchInfo.SourceException);

        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_HandleException_BySettingNull()
    {
        // Arrange
        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();

                Assert.Same(Exception, context.Exception);
                Assert.Same(Exception, context.ExceptionDispatchInfo.SourceException);

                context.Exception = null;
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object }, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_ThrowsUnhandledException()
    {
        // Arrange
        var expected = new DataMisalignedException();

        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resourceFilter2 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter2
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>((c, next) =>
            {
                throw expected;
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, exception: Exception);

        // Act
        var exception = await Assert.ThrowsAsync<DataMisalignedException>(invoker.InvokeAsync);

        // Assert
        Assert.Same(expected, exception);
        Assert.Same(expected, context.Exception);
        Assert.Same(expected, context.ExceptionDispatchInfo.SourceException);
    }

    [Fact]
    public async Task InvokeAction_InvokesResourceFilter_OnResourceExecuting_ThrowsUnhandledException()
    {
        // Arrange
        var expected = new DataMisalignedException();

        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resourceFilter2 = new Mock<IResourceFilter>(MockBehavior.Strict);
        resourceFilter2
            .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
            .Callback<ResourceExecutingContext>((c) =>
            {
                throw expected;
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, exception: Exception);

        // Act
        var exception = await Assert.ThrowsAsync<DataMisalignedException>(invoker.InvokeAsync);

        // Assert
        Assert.Same(expected, exception);
        Assert.Same(expected, context.Exception);
        Assert.Same(expected, context.ExceptionDispatchInfo.SourceException);
    }

    [Fact]
    public async Task InvokeAction_InvokesResourceFilter_OnResourceExecuted_ThrowsUnhandledException()
    {
        // Arrange
        var expected = new DataMisalignedException();

        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resourceFilter2 = new Mock<IResourceFilter>(MockBehavior.Loose);
        resourceFilter2
            .Setup(f => f.OnResourceExecuted(It.IsAny<ResourceExecutedContext>()))
            .Callback<ResourceExecutedContext>((c) =>
            {
                throw expected;
            })
            .Verifiable();

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, exception: Exception);

        // Act
        var exception = await Assert.ThrowsAsync<DataMisalignedException>(invoker.InvokeAsync);

        // Assert
        Assert.Same(expected, exception);
        Assert.Same(expected, context.Exception);
        Assert.Same(expected, context.ExceptionDispatchInfo.SourceException);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_ShortCircuit()
    {
        // Arrange
        var expected = new Mock<IActionResult>(MockBehavior.Strict);
        expected
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resourceFilter2 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter2
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>((c, next) =>
            {
                c.Result = expected.Object;
                return Task.FromResult(true);
            })
            .Verifiable();

        var resourceFilter3 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1.Object, // This filter should see the result returned from resourceFilter2
                    resourceFilter2.Object, // This filter will short circuit
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    exceptionFilter.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
            },
            // The action won't run
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        Assert.Same(expected.Object, context.Result);
        Assert.True(context.Canceled);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_ShortCircuit_WithoutResult()
    {
        // Arrange
        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            })
            .Verifiable();

        var resourceFilter2 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter2
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>((c, next) =>
            {
                return Task.FromResult(true);
            })
            .Verifiable();

        var resourceFilter3 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1.Object, // This filter should see the result returned from resourceFilter2
                    resourceFilter2.Object, // This filter will short circuit
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    exceptionFilter.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
            },
            // The action won't run
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Null(context.Result);
        Assert.True(context.Canceled);
    }

    [Fact]
    public async Task InvokeAction_InvokesResourceFilter_ShortCircuit()
    {
        // Arrange
        var expected = new Mock<IActionResult>(MockBehavior.Strict);
        expected
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        ResourceExecutedContext context = null;
        var resourceFilter1 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter1
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                context = await next();
            });

        var resourceFilter2 = new Mock<IResourceFilter>(MockBehavior.Strict);
        resourceFilter2
            .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
            .Callback<ResourceExecutingContext>((c) =>
            {
                c.Result = expected.Object;
            });

        var resourceFilter3 = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1.Object, // This filter should see the result returned from resourceFilter2
                    resourceFilter2.Object,
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
            },
            // The action won't run
            exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        Assert.Same(expected.Object, context.Result);
        Assert.True(context.Canceled);
    }

    [Fact]
    public async Task InvokeAction_InvokesAsyncResourceFilter_InvalidShortCircuit()
    {
        // Arrange
        var message =
            "If an IAsyncResourceFilter provides a result value by setting the Result property of " +
            "ResourceExecutingContext to a non-null value, then it cannot call the next filter by invoking " +
            "ResourceExecutionDelegate.";

        ResourceExecutedContext context = null;
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                // This is not valid.
                c.Result = Mock.Of<IActionResult>();
                context = await next();
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(invoker.InvokeAsync);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task InvokeAction_AuthorizationFilter_ChallengePreventsResourceFiltersFromRunning()
    {
        // Arrange
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                await next();
            })
            .Verifiable();

        var authorizationFilter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>((c) =>
            {
                c.Result = Result;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resourceFilter.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(
            f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
            Times.Never());
    }

    [Fact]
    public async Task InvokeAction_AuthorizationFilterShortCircuit_InvokesAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();
        resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();

        var authorizationFilter = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter
            .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()))
            .Returns<AuthorizationFilterContext>((c) =>
            {
                c.Result = Result;
                return Task.CompletedTask;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resultFilter.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_AuthorizationFilterShortCircuit_InvokesAsyncAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) =>
            {
                Assert.Same(Result, c.Result);
                return next();
            })
            .Verifiable();

        var authorizationFilter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>((c) =>
            {
                c.Result = Result;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resultFilter.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_AuthorizationFilterShortCircuit_DoesNotRunResultFilters()
    {
        // Arrange
        var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()));
        resultFilter1.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()));

        var resultFilter2 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        resultFilter2.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()));

        var resultFilter3 = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter3.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns(Task.CompletedTask);

        var authorizationFilter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback<AuthorizationFilterContext>((c) =>
            {
                c.Result = Result;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resultFilter1.Object, resultFilter2.Object, resultFilter3.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter1.Verify(
            f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()),
            Times.Never());
        resultFilter1.Verify(
            f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()),
            Times.Never());
        resultFilter2.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Never());
        resultFilter3.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_ResourceFilterShortCircuit_InvokesAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();
        resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();

        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>((c, next) =>
            {
                c.Result = Result;
                return Task.CompletedTask;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, resultFilter.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_ResourceFilterShortCircuit_InvokesAsyncAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) =>
            {
                Assert.Same(Result, c.Result);
                return next();
            })
            .Verifiable();

        var resourceFilter = new Mock<IResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
            .Callback<ResourceExecutingContext>(c => c.Result = Result);

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, resultFilter.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_ResourceFilterShortCircuit_DoesNotRunResultFilters()
    {
        // Arrange
        var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()));
        resultFilter1.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()));

        var resultFilter2 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        resultFilter2.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()));

        var resultFilter3 = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter3.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns(Task.CompletedTask);

        var resourceFilter = new Mock<IResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
            .Callback<ResourceExecutingContext>(c => c.Result = Result);

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, resultFilter1.Object, resultFilter2.Object, resultFilter3.Object, });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter1.Verify(
            f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()),
            Times.Never());
        resultFilter1.Verify(
            f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()),
            Times.Never());
        resultFilter2.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Never());
        resultFilter3.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_ExceptionFilterShortCircuit_InvokesAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Callback<ResultExecutingContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();
        resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Callback<ResultExecutedContext>(c => Assert.Same(Result, c.Result))
            .Verifiable();

        var exceptionFilter = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
        exceptionFilter
            .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
            .Returns<ExceptionContext>(c =>
            {
                c.Result = Result;
                return Task.CompletedTask;
            });

        var invoker = CreateInvoker(new IFilterMetadata[] { exceptionFilter.Object, resultFilter.Object, }, Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_ExceptionFilterShortCircuit_InvokesAsyncAlwaysRunResultFilter()
    {
        // Arrange
        var resultFilter = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) =>
            {
                Assert.Same(Result, c.Result);
                return next();
            })
            .Verifiable();

        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        exceptionFilter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => c.Result = Result);

        var invoker = CreateInvoker(new IFilterMetadata[] { exceptionFilter.Object, resultFilter.Object, }, Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter.Verify();
    }

    [Fact]
    public async Task InvokeAction_ExceptionFilterShortCircuit_DoesNotRunResultFilters()
    {
        // Arrange
        var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()));
        resultFilter1.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()));

        var resultFilter2 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        resultFilter2.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()));

        var resultFilter3 = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter3.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns(Task.CompletedTask);

        var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
        exceptionFilter
            .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
            .Callback<ExceptionContext>(c => c.Result = Result);

        var invoker = CreateInvoker(
            new IFilterMetadata[] { exceptionFilter.Object, resultFilter1.Object, resultFilter2.Object, resultFilter3.Object, },
            Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter1.Verify(
            f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()),
            Times.Never());
        resultFilter1.Verify(
            f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()),
            Times.Never());
        resultFilter2.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Never());
        resultFilter3.Verify(
            f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
            Times.Once());
    }

    [Fact]
    public async Task InvokeAction_AlwaysRunResultFiltersAndRunWithResultFilters()
    {
        // Arrange
        var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
        resultFilter1
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Verifiable();
        resultFilter1
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var resultFilter2 = new Mock<IAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter2
            .Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()))
            .Verifiable();
        resultFilter2
            .Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()))
            .Verifiable();

        var resultFilter3 = new Mock<IAsyncResultFilter>(MockBehavior.Strict);
        resultFilter3.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) => next())
            .Verifiable();

        var resultFilter4 = new Mock<IAsyncAlwaysRunResultFilter>(MockBehavior.Strict);
        resultFilter4.Setup(f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()))
            .Returns<ResultExecutingContext, ResultExecutionDelegate>((c, next) => next())
            .Verifiable();

        var invoker = CreateInvoker(
            new IFilterMetadata[] { resultFilter1.Object, resultFilter2.Object, resultFilter3.Object, resultFilter4.Object });

        // Act
        await invoker.InvokeAsync();

        // Assert
        resultFilter1.Verify();
        resultFilter2.Verify();
        resultFilter3.Verify();
        resultFilter4.Verify();
    }

    public class TestResult : ActionResult
    {
    }
}
