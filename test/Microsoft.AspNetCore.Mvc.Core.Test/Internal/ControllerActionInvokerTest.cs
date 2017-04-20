// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionInvokerTest
    {
        // Intentionally choosing an uncommon exception type.
        private readonly Exception _actionException = new DivideByZeroException();

        private readonly ContentResult _result = new ContentResult() { Content = "Hello, world!" };

        private readonly TestController _controller = new TestController();

        [Fact]
        public async Task InvokeAction_DoesNotInvokeExceptionFilter_WhenActionDoesNotThrow()
        {
            // Arrange
            var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Verifiable();

            var invoker = CreateInvoker(filter.Object, actionThrows: false);

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

            var invoker = CreateInvoker(filter.Object, actionThrows: false);

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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());

            Assert.Same(_actionException, exception);
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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter2.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());

            Assert.Same(_actionException, exception);
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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, actionThrows: true);

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
                actionThrows: true);

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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, actionThrows: true);

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
                actionThrows: true);

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
            var invoker = CreateInvoker(filterMetadata, actionThrows: true);

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
                actionThrows: true);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object }, actionThrows: true);

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
                actionThrows: true);

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
                actionThrows: true);

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

            var invoker = CreateInvoker(filter.Object, actionThrows: true);

            // Act
            await Assert.ThrowsAsync(_actionException.GetType(), async () => await invoker.InvokeAsync());

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

            var invoker = CreateInvoker(new IFilterMetadata[] { filter.Object, resultFilter.Object }, actionThrows: true);

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
                actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter1.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
            filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
            result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
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
            Assert.False(invoker.ControllerFactory.CreateCalled);
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

            Assert.False(invoker.ControllerFactory.CreateCalled);
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
            var actionFilter = new Mock<IActionFilter>(MockBehavior.Strict);
            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilterMetadata[]
            {
                exceptionFilter.Object,
                authorizationFilter1.Object,
                authorizationFilter2.Object,
                resourceFilter.Object,
                actionFilter.Object,
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
        public async Task InvokeAction_InvokesActionFilter()
        {
            // Arrange
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(filter.Object);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            filter.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            Assert.Same(_result, result);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter()
        {
            // Arrange
            IActionResult result = null;

            var filter = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>(async (context, next) =>
                {
                    var resultContext = await next();
                    result = resultContext.Result;
                })
                .Verifiable();

            var invoker = CreateInvoker(filter.Object);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(
                f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
                Times.Once());

            Assert.Same(_result, result);
        }

        [Fact]
        public async Task InvokeAction_InvokesActionFilter_ShortCircuit()
        {
            // Arrange
            var result = new Mock<IActionResult>(MockBehavior.Strict);
            result
                .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

            ActionExecutedContext context = null;

            var actionFilter1 = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter1.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            actionFilter1
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => context = c)
                .Verifiable();

            var actionFilter2 = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter2
                .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
                .Callback<ActionExecutingContext>(c => c.Result = result.Object)
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilterMetadata[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeAsync();

            // Assert
            result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            actionFilter2.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter2.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Never());

            resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            Assert.True(context.Canceled);
            Assert.Same(context.Result, result.Object);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter_ShortCircuit_WithResult()
        {
            // Arrange
            var result = new Mock<IActionResult>(MockBehavior.Strict);
            result
                .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
                .Returns(Task.FromResult(true))
                .Verifiable();

            ActionExecutedContext context = null;

            var actionFilter1 = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter1.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            actionFilter1
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => context = c)
                .Verifiable();

            var actionFilter2 = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            actionFilter2
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>((c, next) =>
                {
                    // Notice we're not calling next
                    c.Result = result.Object;
                    return Task.FromResult(true);
                })
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter1 = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter1.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter1.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();
            var resultFilter2 = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter2.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter2.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilterMetadata[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter1.Object,
                resultFilter2.Object,
            });

            // Act
            await invoker.InvokeAsync();

            // Assert
            result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            actionFilter2.Verify(
                f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
                Times.Once());

            resultFilter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());
            resultFilter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter2.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            Assert.True(context.Canceled);
            Assert.Same(context.Result, result.Object);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter_ShortCircuit_WithoutResult()
        {
            // Arrange
            ActionExecutedContext context = null;

            var actionFilter1 = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter1.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            actionFilter1
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => context = c)
                .Verifiable();

            var actionFilter2 = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            actionFilter2
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>((c, next) =>
                {
                    // Notice we're not calling next
                    return Task.FromResult(true);
                })
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilterMetadata[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeAsync();

            // Assert
            actionFilter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            actionFilter2.Verify(
                f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
                Times.Once());

            resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            Assert.True(context.Canceled);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter_ShortCircuit_WithResult_CallNext()
        {
            // Arrange
            var actionFilter = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            actionFilter
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>(async (c, next) =>
                {
                    c.Result = new EmptyResult();
                    await next();
                })
                .Verifiable();

            var message =
                "If an IAsyncActionFilter provides a result value by setting the Result property of " +
                "ActionExecutingContext to a non-null value, then it cannot call the next filter by invoking " +
                "ActionExecutionDelegate.";

            var invoker = CreateInvoker(actionFilter.Object);

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () => await invoker.InvokeAsync(),
                message);
        }

        [Fact]
        public async Task InvokeAction_InvokesActionFilter_WithExceptionThrownByAction()
        {
            // Arrange
            ActionExecutedContext context = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c =>
                {
                    context = c;

                    // Handle the exception so the test doesn't throw.
                    Assert.False(c.ExceptionHandled);
                    c.ExceptionHandled = true;
                })
                .Verifiable();

            var invoker = CreateInvoker(filter.Object, actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            filter.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            Assert.Same(_actionException, context.Exception);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task InvokeAction_InvokesActionFilter_WithExceptionThrownByActionFilter()
        {
            // Arrange
            var exception = new DataMisalignedException();
            ActionExecutedContext context = null;

            var filter1 = new Mock<IActionFilter>(MockBehavior.Strict);
            filter1.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter1
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c =>
                {
                    context = c;

                    // Handle the exception so the test doesn't throw.
                    Assert.False(c.ExceptionHandled);
                    c.ExceptionHandled = true;
                })
                .Verifiable();

            var filter2 = new Mock<IActionFilter>(MockBehavior.Strict);
            filter2
                .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
                .Callback<ActionExecutingContext>(c => { throw exception; })
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            filter2.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            filter2.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Never());

            Assert.Same(exception, context.Exception);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter_WithExceptionThrownByActionFilter()
        {
            // Arrange
            var exception = new DataMisalignedException();
            ActionExecutedContext context = null;

            var filter1 = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            filter1
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>(async (c, next) =>
                {
                    context = await next();

                    // Handle the exception so the test doesn't throw.
                    Assert.False(context.ExceptionHandled);
                    context.ExceptionHandled = true;
                })
                .Verifiable();

            var filter2 = new Mock<IActionFilter>(MockBehavior.Strict);
            filter2.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter2
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => { throw exception; })
                .Verifiable();

            var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter1.Verify(
                f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
                Times.Once());

            filter2.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());

            Assert.Same(exception, context.Exception);
            Assert.Null(context.Result);
        }

        [Fact]
        public async Task InvokeAction_InvokesActionFilter_HandleException()
        {
            // Arrange
            var result = new Mock<IActionResult>(MockBehavior.Strict);
            result
                .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
                .Returns<ActionContext>((context) => Task.FromResult(true))
                .Verifiable();

            var actionFilter = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            actionFilter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c =>
                {
                    // Handle the exception so the test doesn't throw.
                    Assert.False(c.ExceptionHandled);
                    c.ExceptionHandled = true;

                    c.Result = result.Object;
                })
                .Verifiable();

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(
                new IFilterMetadata[] { actionFilter.Object, resultFilter.Object },
                actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            actionFilter.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
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

            var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            filter2.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
                Times.Once());

            Assert.True(context.Canceled);
            Assert.IsType<ContentResult>(context.Result);
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

            var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            filter2.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
                Times.Once());

            Assert.True(context.Canceled);
            Assert.IsType<ContentResult>(context.Result);
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
                async () => await invoker.InvokeAsync(),
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
            await Assert.ThrowsAsync(exception.GetType(), async () => await invoker.InvokeAsync());

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

            var invoker = CreateInvoker(resourceFilter.Object);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Same(_result, context.Result);

            resourceFilter.Verify(
                f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncResourceFilter_WithActionResult_FromActionFilter()
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

            var actionFilter = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter
                .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
                .Callback<ActionExecutingContext>((c) =>
                {
                    c.Result = expected;
                });

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, actionFilter.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Same(expected, context.Result);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, exceptionFilter.Object }, actionThrows: true);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object }, actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Same(_actionException, context.Exception);
            Assert.Same(_actionException, context.ExceptionDispatchInfo.SourceException);

            resourceFilter.Verify(
                f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncResourceFilter_HandleException_FromActionFilter()
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

            var actionFilter = new Mock<IActionFilter>(MockBehavior.Strict);
            actionFilter
                .Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()))
                .Callback<ActionExecutingContext>((c) =>
                {
                    throw expected;
                });

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, actionFilter.Object });

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
        public async Task InvokeAction_InvokesAsyncResourceFilter_HandlesException_FromExceptionFilter()
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

            var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            exceptionFilter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>((c) =>
                {
                    throw expected;
                });

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, exceptionFilter.Object }, actionThrows: true);

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

                    Assert.Same(_actionException, context.Exception);
                    Assert.Same(_actionException, context.ExceptionDispatchInfo.SourceException);

                    context.Exception = null;
                })
                .Verifiable();

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object }, actionThrows: true);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, actionThrows: true);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, actionThrows: true);

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

            var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter1.Object, resourceFilter2.Object }, actionThrows: true);

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
            var actionFilter = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(
                new IFilterMetadata[]
                {
                    resourceFilter1.Object, // This filter should see the result retured from resourceFilter2
                    resourceFilter2.Object, // This filter will short circuit
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    exceptionFilter.Object, // This shouldn't run - it will throw if it does
                    actionFilter.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
                },
                // The action won't run
                actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            Assert.Same(expected.Object, context.Result);
            Assert.True(context.Canceled);
            Assert.False(invoker.ControllerFactory.CreateCalled);
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
            var actionFilter = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(
                new IFilterMetadata[]
                {
                    resourceFilter1.Object, // This filter should see the result retured from resourceFilter2
                    resourceFilter2.Object, // This filter will short circuit
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    exceptionFilter.Object, // This shouldn't run - it will throw if it does
                    actionFilter.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
                },
                // The action won't run
                actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Null(context.Result);
            Assert.True(context.Canceled);
            Assert.False(invoker.ControllerFactory.CreateCalled);
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
            var actionFilter = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            var resultFilter = new Mock<IAsyncResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(
                new IFilterMetadata[]
                {
                    resourceFilter1.Object, // This filter should see the result retured from resourceFilter2
                    resourceFilter2.Object,
                    resourceFilter3.Object, // This shouldn't run - it will throw if it does
                    actionFilter.Object, // This shouldn't run - it will throw if it does
                    resultFilter.Object // This shouldn't run - it will throw if it does
                },
                // The action won't run
                actionThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            Assert.Same(expected.Object, context.Result);
            Assert.True(context.Canceled);
            Assert.False(invoker.ControllerFactory.CreateCalled);
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
                    c.Result = _result;
                });

            var invoker = CreateInvoker(new IFilterMetadata[] { authorizationFilter.Object, resourceFilter.Object, });

            // Act
            await invoker.InvokeAsync();

            // Assert
            resourceFilter.Verify(
                f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()),
                Times.Never());

            Assert.False(invoker.ControllerFactory.CreateCalled);
        }

        [Fact]
        public async Task AddingValueProviderFactory_AtResourceFilter_IsAvailableInControllerContext()
        {
            // Arrange
            var valueProviderFactory2 = Mock.Of<IValueProviderFactory>();
            var resourceFilter = new Mock<IResourceFilter>();
            resourceFilter
                .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
                .Callback<ResourceExecutingContext>((resourceExecutingContext) =>
                {
                    resourceExecutingContext.ValueProviderFactories.Add(valueProviderFactory2);
                });
            var valueProviderFactory1 = Mock.Of<IValueProviderFactory>();
            var valueProviderFactories = new List<IValueProviderFactory>();
            valueProviderFactories.Add(valueProviderFactory1);

            var invoker = CreateInvoker(
                new IFilterMetadata[] { resourceFilter.Object }, valueProviderFactories: valueProviderFactories);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var controllerContext = invoker.ControllerFactory.ControllerContext;
            Assert.NotNull(controllerContext);
            Assert.Equal(2, controllerContext.ValueProviderFactories.Count);
            Assert.Same(valueProviderFactory1, controllerContext.ValueProviderFactories[0]);
            Assert.Same(valueProviderFactory2, controllerContext.ValueProviderFactories[1]);
        }

        [Fact]
        public async Task DeletingValueProviderFactory_AtResourceFilter_IsNotAvailableInControllerContext()
        {
            // Arrange
            var resourceFilter = new Mock<IResourceFilter>();
            resourceFilter
                .Setup(f => f.OnResourceExecuting(It.IsAny<ResourceExecutingContext>()))
                .Callback<ResourceExecutingContext>((resourceExecutingContext) =>
                {
                    resourceExecutingContext.ValueProviderFactories.RemoveAt(0);
                });
            var valueProviderFactory1 = Mock.Of<IValueProviderFactory>();
            var valueProviderFactory2 = Mock.Of<IValueProviderFactory>();
            var valueProviderFactories = new List<IValueProviderFactory>();
            valueProviderFactories.Add(valueProviderFactory1);
            valueProviderFactories.Add(valueProviderFactory2);

            var invoker = CreateInvoker(
                new IFilterMetadata[] { resourceFilter.Object }, valueProviderFactories: valueProviderFactories);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var controllerContext = invoker.ControllerFactory.ControllerContext;
            Assert.NotNull(controllerContext);
            Assert.Equal(1, controllerContext.ValueProviderFactories.Count);
            Assert.Same(valueProviderFactory2, controllerContext.ValueProviderFactories[0]);
        }

        [Fact]
        public async Task MaxAllowedErrorsIsSet_BeforeCallingAuthorizationFilter()
        {
            // Arrange
            var expected = 147;
            var filter = new MockAuthorizationFilter(expected);
            var invoker = CreateInvoker(
                filter,
                actionThrows: false,
                maxAllowedErrorsInModelState: expected);

            // Act & Assert
            // The authorization filter asserts if MaxAllowedErrors was set to the right value.
            await invoker.InvokeAsync();
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_TaskReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.TaskAction), actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.IsType<EmptyResult>(result);
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_TaskOfValueReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.TaskValueTypeAction), actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(inputParam1, contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_WithAsyncKeywordThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.TaskActionWithException), actionParameters);

            // Act and Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                    () => invoker.InvokeAsync());
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_WithoutAsyncThrows()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.TaskActionWithExceptionWithoutAsync), actionParameters);

            // Act and Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                    () => invoker.InvokeAsync());
        }

        public async Task InvokeAction_AsyncAction_WithExceptionsAfterAwait()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.TaskActionThrowAfterAwait), actionParameters);
            var expectedException = "Argument Exception";

            // Act and Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => invoker.InvokeAsync());
            Assert.Equal(expectedException, ex.Message);
        }

        [Fact]
        public async Task InvokeAction_SyncAction()
        {
            // Arrange
            var inputString = "hello";
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.Echo), new Dictionary<string, object>() { { "input", inputString } });

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(inputString, contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_SyncAction_WithException()
        {
            // Arrange
            var inputString = "hello";
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.EchoWithException),
                new Dictionary<string, object>() { { "input", inputString } });

            // Act & Assert
            await Assert.ThrowsAsync<NotImplementedException>(
                () => invoker.InvokeAsync());
        }

        [Fact]
        public async Task InvokeAction_SyncMethod_WithArgumentDictionary_DefaultValueAttributeUsed()
        {
            // Arrange
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.EchoWithDefaultValue),
                new Dictionary<string, object>());

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal("hello", contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_SyncMethod_WithArgumentArray_DefaultValueAttributeIgnored()
        {
            // Arrange
            var inputString = "test";
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.EchoWithDefaultValue),
                new Dictionary<string, object>() { { "input", inputString } });

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(inputString, contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_SyncMethod_WithArgumentDictionary_DefaultParameterValueUsed()
        {
            // Arrange
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.EchoWithDefaultValueAndAttribute),
                new Dictionary<string, object>());

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal("world", contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_SyncMethod_WithArgumentDictionary_AnyValue_HasPrecedenceOverDefaults()
        {
            // Arrange
            var inputString = "test";
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.EchoWithDefaultValueAndAttribute),
                new Dictionary<string, object>() { { "input", inputString } });

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(inputString, contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_WithCustomTaskReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.TaskActionWithCustomTaskReturnType),
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.IsType(typeof(EmptyResult), result);
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_WithCustomTaskOfTReturnType()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.TaskActionWithCustomTaskOfTReturnType),
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.IsType(typeof(ObjectResult), result);
            Assert.IsType(typeof(int), ((ObjectResult)result).Value);
            Assert.Equal(1, ((ObjectResult)result).Value);
        }

        [Fact]
        public async Task InvokeAction_AsyncAction_ReturningUnwrappedTask()
        {
            // Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";
            var actionParameters = new Dictionary<string, object> { { "i", inputParam1 }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.UnwrappedTask), actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.IsType<EmptyResult>(result);
        }

        [Fact]
        public async Task InvokeAction_AsyncActionWithTaskOfObjectReturnType_AndReturningTaskOfActionResult()
        {
            // Arrange
            var actionParameters = new Dictionary<string, object> { ["value"] = 3 };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result);

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.AsyncActionMethodReturningActionResultWithTaskOfObjectAsReturnType),
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var testResult = Assert.IsType<TestActionResult>(result);
            Assert.Equal(3, testResult.Value);
        }

        [Fact]
        public async Task InvokeAction_ActionWithObjectReturnType_AndReturningActionResult()
        {
            // Arrange
            var actionParameters = new Dictionary<string, object> { ["value"] = 3 };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result);

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.ActionMethodReturningActionResultWithObjectAsReturnType),
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var testResult = Assert.IsType<TestActionResult>(result);
            Assert.Equal(3, testResult.Value);
        }

        [Fact]
        public async Task InvokeAction_AsyncMethod_ParametersInRandomOrder()
        {
            //Arrange
            var inputParam1 = 1;
            var inputParam2 = "Second Parameter";

            // Note that the order of parameters is reversed
            var actionParameters = new Dictionary<string, object> { { "s", inputParam2 }, { "i", inputParam1 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.TaskValueTypeAction),
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(inputParam1, contentResult.Value);
        }

        [Theory]
        [InlineData(nameof(TestController.AsynActionMethodWithTestActionResult))]
        [InlineData(nameof(TestController.ActionMethodWithTestActionResult))]
        public async Task InvokeAction_ReturnTypeAsIActionResult_ReturnsExpected(string methodName)
        {
            //Arrange
            var inputParam = 1;
            var actionParameters = new Dictionary<string, object> { { "value", inputParam } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                methodName,
                actionParameters);

            // Act
            await invoker.InvokeAsync();

            // Assert
            var contentResult = Assert.IsType<TestActionResult>(result);
            Assert.Equal(inputParam, contentResult.Value);
        }

        [Fact]
        public async Task InvokeAction_AsyncMethod_InvalidParameterValueThrows()
        {
            //Arrange
            var inputParam2 = "Second Parameter";

            var actionParameters = new Dictionary<string, object> { { "i", "Some Invalid Value" }, { "s", inputParam2 } };
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                nameof(TestController.TaskValueTypeAction),
                actionParameters);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCastException>(
                () => invoker.InvokeAsync());
        }

        [Theory]
        [InlineData(nameof(TestController.ActionMethodWithNullActionResult), typeof(IActionResult))]
        [InlineData(nameof(TestController.TestActionMethodWithNullActionResult), typeof(TestActionResult))]
        [InlineData(nameof(TestController.AsyncActionMethodWithNullActionResult), typeof(IActionResult))]
        [InlineData(nameof(TestController.AsyncActionMethodWithNullTestActionResult), typeof(TestActionResult))]
        [ReplaceCulture]
        public async Task InvokeAction_WithNullActionResultThrows(string methodName, Type resultType)
        {
            // Arrange
            IActionResult result = null;

            var filter = new Mock<IActionFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => result = c.Result)
                .Verifiable();

            var invoker = CreateInvoker(
                new[] { filter.Object },
                methodName,
                new Dictionary<string, object>());

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                () => invoker.InvokeAsync(),
                $"Cannot return null from an action method with a return type of '{resultType}'.");
        }

        [Fact]
        public async Task Invoke_UsesDefaultValuesIfNotBound()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
                BoundProperties = new List<ParameterDescriptor>(),
                MethodInfo = typeof(TestController).GetTypeInfo()
                    .DeclaredMethods
                    .First(m => m.Name.Equals("ActionMethodWithDefaultValues", StringComparison.Ordinal)),

                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor
                    {
                        Name = "value",
                        ParameterType = typeof(int),
                        BindingInfo = new BindingInfo(),
                    }
                },
                FilterDescriptors = new List<FilterDescriptor>()
            };

            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Items)
                .Returns(new Dictionary<object, object>());
            context.Setup(c => c.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(new NullLoggerFactory());

            var actionContext = new ActionContext(context.Object, new RouteData(), actionDescriptor);

            var controllerFactory = new Mock<IControllerFactory>();
            controllerFactory.Setup(c => c.CreateController(It.IsAny<ControllerContext>()))
                .Returns(new TestController());

            var metadataProvider = new EmptyModelMetadataProvider();

            var parameterBinder = new ParameterBinder(
                metadataProvider,
                TestModelBinderFactory.CreateDefault(metadataProvider),
                new DefaultObjectValidator(metadataProvider, new IModelValidatorProvider[0]));

            var controllerContext = new ControllerContext(actionContext)
            {
                ValueProviderFactories = new IValueProviderFactory[0]
            };
            controllerContext.ModelState.MaxAllowedErrors = 200;

            var invoker = new ControllerActionInvoker(
                controllerFactory.Object,
                parameterBinder,
                metadataProvider,
                new NullLoggerFactory().CreateLogger<ControllerActionInvoker>(),
                new DiagnosticListener("Microsoft.AspNetCore"),
                controllerContext,
                new IFilterMetadata[0],
                ObjectMethodExecutor.Create(
                    actionDescriptor.MethodInfo,
                    actionDescriptor.ControllerTypeInfo,
                    ParameterDefaultValues.GetParameterDefaultValues(actionDescriptor.MethodInfo)));

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Equal(5, context.Object.Items["Result"]);
        }

        [Fact]
        public async Task Invoke_Success_LogsCorrectValues()
        {
            // Arrange
            var sink = new TestSink();
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            var logger = loggerFactory.CreateLogger<ControllerActionInvoker>();

            var displayName = "A.B.C";
            var mockActionDescriptor = new Mock<ControllerActionDescriptor>();
            mockActionDescriptor
                .SetupGet(ad => ad.DisplayName)
                .Returns(displayName);
            var actionDescriptor = mockActionDescriptor.Object;
            actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest).GetMethod(
                    nameof(ControllerActionInvokerTest.ActionMethod));
            actionDescriptor.ControllerTypeInfo = typeof(ControllerActionInvokerTest).GetTypeInfo();
            actionDescriptor.FilterDescriptors = new List<FilterDescriptor>();
            actionDescriptor.Parameters = new List<ParameterDescriptor>();
            actionDescriptor.BoundProperties = new List<ParameterDescriptor>();

            var filter = Mock.Of<IFilterMetadata>();
            var invoker = CreateInvoker(
                new[] { filter },
                actionDescriptor,
                parameterBinder: null,
                controller: null,
                logger: logger);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.Single(sink.Scopes);
            Assert.Equal(displayName, sink.Scopes[0].Scope?.ToString());

            Assert.Equal(4, sink.Writes.Count);
            Assert.Equal($"Executing action {displayName}", sink.Writes[0].State?.ToString());
            Assert.Equal($"Executing action method {displayName} with arguments ((null)) - ModelState is Valid", sink.Writes[1].State?.ToString());
            Assert.Equal($"Executed action method {displayName}, returned result Microsoft.AspNetCore.Mvc.ContentResult.", sink.Writes[2].State?.ToString());
            // This message has the execution time embedded, which we don't want to verify.
            Assert.StartsWith($"Executed action {displayName} ", sink.Writes[3].State?.ToString());
        }

        [Fact]
        public async Task Invoke_WritesDiagnostic_ActionSelected()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
                BoundProperties = new List<ParameterDescriptor>(),
            };

            actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest).GetMethod(
                    nameof(ControllerActionInvokerTest.ActionMethod));
            actionDescriptor.ControllerTypeInfo = typeof(ControllerActionInvokerTest).GetTypeInfo();

            var listener = new TestDiagnosticListener();

            var routeData = new RouteData();
            routeData.Values.Add("tag", "value");

            var filter = Mock.Of<IFilterMetadata>();
            var invoker = CreateInvoker(
                new[] { filter },
                actionDescriptor,
                parameterBinder: null,
                controller: null,
                diagnosticListener: listener,
                routeData: routeData);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.NotNull(listener.BeforeAction?.ActionDescriptor);
            Assert.NotNull(listener.BeforeAction?.HttpContext);

            var routeValues = listener.BeforeAction?.RouteData?.Values;
            Assert.NotNull(routeValues);

            Assert.Equal(1, routeValues.Count);
            Assert.Contains(routeValues, kvp => kvp.Key == "tag" && string.Equals(kvp.Value, "value"));
        }

        [Fact]
        public async Task Invoke_WritesDiagnostic_ActionInvoked()
        {
            // Arrange
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
                BoundProperties = new List<ParameterDescriptor>(),
            };

            actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest).GetMethod(
                    nameof(ControllerActionInvokerTest.ActionMethod));
            actionDescriptor.ControllerTypeInfo = typeof(ControllerActionInvokerTest).GetTypeInfo();

            var listener = new TestDiagnosticListener();

            var filter = Mock.Of<IFilterMetadata>();
            var invoker = CreateInvoker(
                new[] { filter },
                actionDescriptor,
                parameterBinder: null,
                controller: null,
                diagnosticListener: listener);

            // Act
            await invoker.InvokeAsync();

            // Assert
            Assert.NotNull(listener.AfterAction?.ActionDescriptor);
            Assert.NotNull(listener.AfterAction?.HttpContext);
        }

        [Fact]
        public async Task InvokeAction_ExceptionBubbling_AsyncActionFilter_To_ResourceFilter()
        {
            // Arrange
            var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
            resourceFilter
                .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
                .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
                {
                    var context = await next();
                    Assert.Same(_actionException, context.Exception);
                    context.ExceptionHandled = true;
                });

            var actionFilter1 = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            actionFilter1
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>(async (c, next) =>
                {
                    await next();
                });

            var actionFilter2 = new Mock<IAsyncActionFilter>(MockBehavior.Strict);
            actionFilter2
                .Setup(f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()))
                .Returns<ActionExecutingContext, ActionExecutionDelegate>(async (c, next) =>
                {
                    await next();
                });

            var invoker = CreateInvoker(
                new IFilterMetadata[]
                {
                    resourceFilter.Object,
                    actionFilter1.Object,
                    actionFilter2.Object,
                },
                // The action won't run
                actionThrows: true);

            // Act & Assert
            await invoker.InvokeAsync();
        }

        private TestControllerActionInvoker CreateInvoker(
            IFilterMetadata filter,
            bool actionThrows = false,
            int maxAllowedErrorsInModelState = 200,
            List<IValueProviderFactory> valueProviderFactories = null)
        {
            return CreateInvoker(new[] { filter }, actionThrows, maxAllowedErrorsInModelState, valueProviderFactories);
        }

        private TestControllerActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            bool actionThrows = false,
            int maxAllowedErrorsInModelState = 200,
            List<IValueProviderFactory> valueProviderFactories = null)
        {
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
                BoundProperties = new List<ParameterDescriptor>(),
            };

            if (actionThrows)
            {
                actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest).GetMethod(
                    nameof(ControllerActionInvokerTest.ThrowingActionMethod));
            }
            else
            {
                actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest).GetMethod(
                    nameof(ControllerActionInvokerTest.ActionMethod));
            }
            actionDescriptor.ControllerTypeInfo = typeof(ControllerActionInvokerTest).GetTypeInfo();

            return CreateInvoker(
                filters, actionDescriptor, null, null, maxAllowedErrorsInModelState, valueProviderFactories);
        }

        private TestControllerActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            string methodName,
            IDictionary<string, object> arguments,
            int maxAllowedErrorsInModelState = 200)
        {
            var actionDescriptor = new ControllerActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
                BoundProperties = new List<ParameterDescriptor>(),
                MethodInfo = typeof(TestController).GetMethod(methodName),
                ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            };

            foreach (var argument in arguments)
            {
                actionDescriptor.Parameters.Add(new ParameterDescriptor
                {
                    Name = argument.Key,
                    ParameterType = argument.Value.GetType(),
                });
            }

            var parameterBinder = new TestParameterBinder(arguments);

            return CreateInvoker(filters, actionDescriptor, _controller, parameterBinder, maxAllowedErrorsInModelState);
        }

        private TestControllerActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            ControllerActionDescriptor actionDescriptor,
            object controller,
            ParameterBinder parameterBinder = null,
            int maxAllowedErrorsInModelState = 200,
            List<IValueProviderFactory> valueProviderFactories = null,
            RouteData routeData = null,
            ILogger logger = null,
            object diagnosticListener = null)
        {
            var httpContext = new DefaultHttpContext();
            var options = new MvcOptions();
            var mvcOptionsAccessor = new TestOptionsManager<MvcOptions>(options);

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<IOptions<MvcOptions>>(mvcOptionsAccessor);
            services.AddSingleton(new ObjectResultExecutor(
                mvcOptionsAccessor,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            services.AddSingleton(new ContentResultExecutor(
                NullLogger<ContentResultExecutor>.Instance,
                new MemoryPoolHttpResponseStreamWriterFactory(ArrayPool<byte>.Shared, ArrayPool<char>.Shared)));

            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = services.BuildServiceProvider();

            var formatter = new Mock<IOutputFormatter>();
            formatter
                .Setup(f => f.CanWriteResult(It.IsAny<OutputFormatterCanWriteContext>()))
                .Returns(true);

            formatter
                .Setup(f => f.WriteAsync(It.IsAny<OutputFormatterWriteContext>()))
                .Returns<OutputFormatterWriteContext>(async c =>
                {
                    await c.HttpContext.Response.WriteAsync(c.Object.ToString());
                });

            options.OutputFormatters.Add(formatter.Object);

            if (routeData == null)
            {
                routeData = new RouteData();
            }

            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: routeData,
                actionDescriptor: actionDescriptor);

            if (parameterBinder == null)
            {
                parameterBinder = new TestParameterBinder(new Dictionary<string, object>());
            }

            if (valueProviderFactories == null)
            {
                valueProviderFactories = new List<IValueProviderFactory>();
            }

            if (logger == null)
            {
                logger = new NullLoggerFactory().CreateLogger<ControllerActionInvoker>();
            }

            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            if (diagnosticListener != null)
            {
                diagnosticSource.SubscribeWithAdapter(diagnosticListener);
            }

            var invoker = new TestControllerActionInvoker(
                filters,
                new MockControllerFactory(controller ?? this),
                parameterBinder,
                TestModelMetadataProvider.CreateDefaultProvider(),
                logger,
                diagnosticSource,
                actionContext,
                valueProviderFactories.AsReadOnly(),
                maxAllowedErrorsInModelState);
            return invoker;
        }

        public IActionResult ActionMethod()
        {
            return _result;
        }

        public ObjectResult ThrowingActionMethod()
        {
            throw _actionException;
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }

        private sealed class TestController
        {
            public IActionResult ActionMethodWithDefaultValues(int value = 5)
            {
                return new TestActionResult { Value = value };
            }

            public TestActionResult ActionMethodWithTestActionResult(int value)
            {
                return new TestActionResult { Value = value };
            }

            public async Task<TestActionResult> AsynActionMethodWithTestActionResult(int value)
            {
                return await Task.FromResult<TestActionResult>(new TestActionResult { Value = value });
            }

            public IActionResult ActionMethodWithNullActionResult()
            {
                return null;
            }

            public object ActionMethodReturningActionResultWithObjectAsReturnType(int value = 5)
            {
                return new TestActionResult { Value = value };
            }

            public async Task<object> AsyncActionMethodReturningActionResultWithTaskOfObjectAsReturnType(int value = 5)
            {
                return await Task.FromResult(new TestActionResult { Value = value });
            }

            public TestActionResult TestActionMethodWithNullActionResult()
            {
                return null;
            }

            public async Task<IActionResult> AsyncActionMethodWithNullActionResult()
            {
                return await Task.FromResult<IActionResult>(null);
            }

            public async Task<TestActionResult> AsyncActionMethodWithNullTestActionResult()
            {
                return await Task.FromResult<TestActionResult>(null);
            }
#pragma warning disable 1998
            public async Task TaskAction(int i, string s)
            {
                return;
            }
#pragma warning restore 1998

#pragma warning disable 1998
            public async Task<int> TaskValueTypeAction(int i, string s)
            {
                return i;
            }
#pragma warning restore 1998

#pragma warning disable 1998
            public async Task<Task<int>> TaskOfTaskAction(int i, string s)
            {
                return TaskValueTypeAction(i, s);
            }
#pragma warning restore 1998

            public Task<int> TaskValueTypeActionWithoutAsync(int i, string s)
            {
                return TaskValueTypeAction(i, s);
            }

#pragma warning disable 1998
            public async Task<int> TaskActionWithException(int i, string s)
            {
                throw new NotImplementedException("Not Implemented Exception");
            }
#pragma warning restore 1998

            public Task<int> TaskActionWithExceptionWithoutAsync(int i, string s)
            {
                throw new NotImplementedException("Not Implemented Exception");
            }

            public async Task<int> TaskActionThrowAfterAwait(int i, string s)
            {
                await Task.Delay(500);
                throw new ArgumentException("Argument Exception");
            }

            public TaskDerivedType TaskActionWithCustomTaskReturnType(int i, string s)
            {
                var task = new TaskDerivedType();
                task.Start();
                return task;
            }

            public TaskOfTDerivedType<int> TaskActionWithCustomTaskOfTReturnType(int i, string s)
            {
                var task = new TaskOfTDerivedType<int>(1);
                task.Start();
                return task;
            }

            /// <summary>
            /// Returns a <see cref="Task{TResult}"/> instead of a <see cref="Task"/>.
            /// </summary>
            public Task UnwrappedTask(int i, string s)
            {
                return Task.Factory.StartNew(async () => await Task.Factory.StartNew(() => i));
            }

            public string Echo(string input)
            {
                return input;
            }

            public string EchoWithException(string input)
            {
                throw new NotImplementedException();
            }

            public string EchoWithDefaultValue([DefaultValue("hello")] string input)
            {
                return input;
            }

            public string EchoWithDefaultValueAndAttribute([DefaultValue("hello")] string input = "world")
            {
                return input;
            }

            public class TaskDerivedType : Task
            {
                public TaskDerivedType()
                    : base(() => Console.WriteLine("In The Constructor"))
                {
                }
            }

            public class TaskOfTDerivedType<T> : Task<T>
            {
                public TaskOfTDerivedType(T input)
                    : base(() => input)
                {
                }
            }
        }

        private sealed class TestActionResult : IActionResult
        {
            public int Value { get; set; }

            public Task ExecuteResultAsync(ActionContext context)
            {
                context.HttpContext.Items["Result"] = Value;
                return Task.FromResult(0);
            }
        }

        private class MockControllerFactory : IControllerFactory
        {
            private object _controller;

            public MockControllerFactory(object controller)
            {
                _controller = controller;
            }

            public bool CreateCalled { get; private set; }

            public bool ReleaseCalled { get; private set; }

            public ControllerContext ControllerContext { get; private set; }

            public object CreateController(ControllerContext context)
            {
                ControllerContext = context;
                CreateCalled = true;
                return _controller;
            }

            public void ReleaseController(ControllerContext context, object controller)
            {
                Assert.NotNull(controller);
                Assert.Same(_controller, controller);
                ReleaseCalled = true;
            }

            public void Verify()
            {
                if (CreateCalled && !ReleaseCalled)
                {
                    Assert.False(true, "ReleaseController should have been called.");
                }
            }
        }

        private class TestControllerActionInvoker : ControllerActionInvoker
        {
            public TestControllerActionInvoker(
                IFilterMetadata[] filters,
                MockControllerFactory controllerFactory,
                ParameterBinder parameterBinder,
                IModelMetadataProvider modelMetadataProvider,
                ILogger logger,
                DiagnosticSource diagnosticSource,
                ActionContext actionContext,
                IReadOnlyList<IValueProviderFactory> valueProviderFactories,
                int maxAllowedErrorsInModelState)
                : base(
                      controllerFactory,
                      parameterBinder,
                      modelMetadataProvider,
                      logger,
                      diagnosticSource,
                      CreatControllerContext(actionContext, valueProviderFactories, maxAllowedErrorsInModelState),
                      filters,
                      CreateExecutor((ControllerActionDescriptor)actionContext.ActionDescriptor))
            {
                ControllerFactory = controllerFactory;
            }

            public MockControllerFactory ControllerFactory { get; }

            public async override Task InvokeAsync()
            {
                await base.InvokeAsync();

                // Make sure that the controller was disposed in every test that creates ones.
                ControllerFactory.Verify();
            }

            private static ObjectMethodExecutor CreateExecutor(ControllerActionDescriptor actionDescriptor)
            {
                return ObjectMethodExecutor.Create(
                    actionDescriptor.MethodInfo,
                    actionDescriptor.ControllerTypeInfo,
                    ParameterDefaultValues.GetParameterDefaultValues(actionDescriptor.MethodInfo));
            }

            private static ControllerContext CreatControllerContext(
                ActionContext actionContext,
                IReadOnlyList<IValueProviderFactory> valueProviderFactories,
                int maxAllowedErrorsInModelState)
            {
                var controllerContext = new ControllerContext(actionContext)
                {
                    ValueProviderFactories = valueProviderFactories.ToList()
                };
                controllerContext.ModelState.MaxAllowedErrors = maxAllowedErrorsInModelState;

                return controllerContext;
            }
        }

        private class MockAuthorizationFilter : IAuthorizationFilter
        {
            int _expectedMaxAllowedErrors;

            public MockAuthorizationFilter(int maxAllowedErrors)
            {
                _expectedMaxAllowedErrors = maxAllowedErrors;
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                Assert.NotNull(context.ModelState.MaxAllowedErrors);
                Assert.Equal(_expectedMaxAllowedErrors, context.ModelState.MaxAllowedErrors);
            }
        }

        private class TestParameterBinder : ParameterBinder
        {
            private readonly IDictionary<string, object> _actionParameters;
            public TestParameterBinder(IDictionary<string, object> actionParameters)
                : base(
                    new EmptyModelMetadataProvider(),
                    TestModelBinderFactory.CreateDefault(),
                    Mock.Of<IObjectModelValidator>())
            {
                _actionParameters = actionParameters;
            }

            public override Task<ModelBindingResult> BindModelAsync(
                ActionContext actionContext,
                IValueProvider valueProvider,
                ParameterDescriptor parameter,
                object value)
            {
                if (_actionParameters.TryGetValue(parameter.Name, out var result))
                {
                    return Task.FromResult(ModelBindingResult.Success(result));
                }

                return Task.FromResult(ModelBindingResult.Failed());
            }
        }
    }
}
