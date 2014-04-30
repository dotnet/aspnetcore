// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionInvokerTest
    {
        // Intentionally choosing an uncommon exception type.
        private readonly Exception _actionException = new TimeZoneNotFoundException();

        private readonly JsonResult _result = new JsonResult(new { message = "Hello, world!" });

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
            await invoker.InvokeActionAsync();

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
                .Returns<ExceptionContext>((context) => Task.FromResult<object>(null))
                .Verifiable();

            var invoker = CreateInvoker(filter.Object, actionThrows: false);

            // Act
            await invoker.InvokeActionAsync();

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
            IActionResult result = null;

            var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(context =>
                {
                    exception = context.Exception;
                    result = context.Result;

                    // Handle the exception
                    context.Result = new EmptyResult();
                })
                .Verifiable();

            var invoker = CreateInvoker(filter.Object, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());

            Assert.Same(_actionException, exception);
            Assert.Null(result);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncExceptionFilter_WhenActionThrows()
        {
            // Arrange
            Exception exception = null;
            IActionResult result = null;

            var filter = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(context =>
                {
                    exception = context.Exception;
                    result = context.Result;

                    // Handle the exception
                    context.Result = new EmptyResult();
                })
                .Returns<ExceptionContext>((context) => Task.FromResult<object>(null))
                .Verifiable();

            var invoker = CreateInvoker(filter.Object, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());

            Assert.Same(_actionException, exception);
            Assert.Null(result);
        }

        [Fact]
        public async Task InvokeAction_InvokesExceptionFilter_ShortCircuit()
        {
            // Arrange
            var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);

            var filter2 = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter2
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(context =>
                {
                    context.Exception = null;
                })
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter2.Verify(
                f => f.OnException(It.IsAny<ExceptionContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncExceptionFilter_ShortCircuit()
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
                .Returns<ExceptionContext>((context) => Task.FromResult<object>(null))
                .Verifiable();

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object }, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter2.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());
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
            await Assert.ThrowsAsync(_actionException.GetType(), async () => await invoker.InvokeActionAsync());

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
                .Returns<ActionContext>((context) => Task.FromResult<object>(null))
                .Verifiable();

            var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(c => c.Result = result.Object)
                .Verifiable();

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { filter.Object, resultFilter.Object }, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
            result.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAuthorizationFilter()
        {
            // Arrange
            var filter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnAuthorization(It.IsAny<AuthorizationContext>())).Verifiable();

            var invoker = CreateInvoker(filter.Object);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncAuthorizationFilter()
        {
            // Arrange
            var filter = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationContext>()))
                .Returns<AuthorizationContext>(context => Task.FromResult<object>(null))
                .Verifiable();

            var invoker = CreateInvoker(filter.Object);

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(
                f => f.OnAuthorizationAsync(It.IsAny<AuthorizationContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAuthorizationFilter_ShortCircuit()
        {
            // Arrange
            var challenge = new Mock<IActionResult>(MockBehavior.Loose).Object;

            var filter1 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            filter1
                .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()))
                .Callback<AuthorizationContext>(c => c.Result = challenge)
                .Verifiable();

            var filter2 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncAuthorizationFilter_ShortCircuit()
        {
            // Arrange
            var challenge = new Mock<IActionResult>(MockBehavior.Loose).Object;

            var filter1 = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
            filter1
                .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationContext>()))
                .Returns<AuthorizationContext>((context) =>
                {
                    context.Result = challenge;
                    return Task.FromResult<object>(null);
                })
                .Verifiable();

            var filter2 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(
                f => f.OnAuthorizationAsync(It.IsAny<AuthorizationContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAction_ExceptionInAuthorizationFilterHandledByExceptionFilters()
        {
            // Arrange
            Exception exception = null;
            var expected = new InvalidCastException();

            var exceptionFilter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            exceptionFilter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(context =>
                {
                    exception = context.Exception;

                    // Mark as handled
                    context.Result = new EmptyResult();
                })
                .Verifiable();

            var authorizationFilter1 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            authorizationFilter1
                .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()))
                .Callback<AuthorizationContext>(c => { throw expected; })
                .Verifiable();

            // None of these filters should run
            var authorizationFilter2 = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            var actionFilter = new Mock<IActionFilter>(MockBehavior.Strict);
            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] 
            { 
                exceptionFilter.Object, 
                authorizationFilter1.Object, 
                authorizationFilter2.Object, 
                actionFilter.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            exceptionFilter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
            authorizationFilter1.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAction_InvokesAuthorizationFilter_ChallengeNotSeenByResultFilters()
        {
            // Arrange
            var challenge = new Mock<IActionResult>(MockBehavior.Strict);
            challenge
                .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
                .Returns<ActionContext>((context) => Task.FromResult<object>(null))
                .Verifiable();

            var authorizationFilter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            authorizationFilter
                .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()))
                .Callback<AuthorizationContext>(c => c.Result = challenge.Object)
                .Verifiable();

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { authorizationFilter.Object, resultFilter.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            authorizationFilter.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationContext>()), Times.Once());
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
            await invoker.InvokeActionAsync();

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
            await invoker.InvokeActionAsync();

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
            var result = new EmptyResult();

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
                .Callback<ActionExecutingContext>(c => c.Result = result)
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilter[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            actionFilter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            actionFilter2.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());

            resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            Assert.True(context.Canceled);
            Assert.Same(context.Result, result);
        }

        [Fact]
        public async Task InvokeAction_InvokesAsyncActionFilter_ShortCircuit_WithResult()
        {
            // Arrange
            var result = new EmptyResult();

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
                    c.Result = result;
                    return Task.FromResult<object>(null);
                })
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilter[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            actionFilter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            actionFilter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            actionFilter2.Verify(
                f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
                Times.Once());

            resultFilter.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            resultFilter.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            Assert.True(context.Canceled);
            Assert.Same(context.Result, result);
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
                    return Task.FromResult<object>(null);
                })
                .Verifiable();

            var actionFilter3 = new Mock<IActionFilter>(MockBehavior.Strict);

            var resultFilter = new Mock<IResultFilter>(MockBehavior.Strict);
            resultFilter.Setup(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>())).Verifiable();
            resultFilter.Setup(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>())).Verifiable();

            var invoker = CreateInvoker(new IFilter[]
            {
                actionFilter1.Object,
                actionFilter2.Object,
                actionFilter3.Object,
                resultFilter.Object,
            });

            // Act
            await invoker.InvokeActionAsync();

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
                async () => await invoker.InvokeActionAsync(), 
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
            await invoker.InvokeActionAsync();

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
            filter2.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
            filter2
                .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
                .Callback<ActionExecutedContext>(c => { throw exception; })
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

            filter2.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());

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

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object });

            // Act
            await invoker.InvokeActionAsync();

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
                .Returns<ActionContext>((context) => Task.FromResult<object>(null))
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

            var invoker = CreateInvoker(new IFilter[] { actionFilter.Object, resultFilter.Object }, actionThrows: true);

            // Act
            await invoker.InvokeActionAsync();

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
            await invoker.InvokeActionAsync();

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
            await invoker.InvokeActionAsync();

            // Assert
            filter.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
                Times.Once());
        }
        
        [Fact]
        public async Task InvokeAction_InvokesResultFilter_ShortCircuit()
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
                .Callback<ResultExecutingContext>(c => c.Cancel = true)
                .Verifiable();

            var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object, filter3.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            filter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());

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
                    return Task.FromResult<object>(null); 
                })
                .Verifiable();

            var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object, filter3.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            filter2.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
                Times.Once());

            Assert.True(context.Canceled);
            Assert.IsType<JsonResult>(context.Result);
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
                    return Task.FromResult<object>(null);
                })
                .Verifiable();

            var filter3 = new Mock<IResultFilter>(MockBehavior.Strict);

            var invoker = CreateInvoker(new IFilter[] { filter1.Object, filter2.Object, filter3.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            filter1.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
            filter1.Verify(f => f.OnResultExecuted(It.IsAny<ResultExecutedContext>()), Times.Once());

            filter2.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()),
                Times.Once());

            Assert.True(context.Canceled);
            Assert.IsType<JsonResult>(context.Result);
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
                async () => await invoker.InvokeActionAsync(),
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
            await Assert.ThrowsAsync(exception.GetType(), async () => await invoker.InvokeActionAsync());

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
            await invoker.InvokeActionAsync();

            // Assert
            Assert.Equal(exception, context.Exception);

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
            await invoker.InvokeActionAsync();

            // Assert
            Assert.Equal(exception, context.Exception);

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

            var invoker = CreateInvoker(new IFilter[] { resultFilter1.Object, resultFilter2.Object, resultFilter3.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            Assert.Equal(exception, context.Exception);

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

            var invoker = CreateInvoker(new IFilter[] { resultFilter1.Object, resultFilter2.Object, resultFilter3.Object });

            // Act
            await invoker.InvokeActionAsync();

            // Assert
            Assert.Equal(exception, context.Exception);

            resultFilter1.Verify(
                f => f.OnResultExecutionAsync(It.IsAny<ResultExecutingContext>(), It.IsAny<ResultExecutionDelegate>()), 
                Times.Once());

            resultFilter2.Verify(f => f.OnResultExecuting(It.IsAny<ResultExecutingContext>()), Times.Once());
        }

        private ReflectedActionInvoker CreateInvoker(IFilter filter, bool actionThrows = false)
        {
            return CreateInvoker(new[] { filter }, actionThrows);
        }

        private ReflectedActionInvoker CreateInvoker(IFilter[] filters, bool actionThrows = false)
        {
            var actionDescriptor = new ReflectedActionDescriptor()
            {
                FilterDescriptors = new List<FilterDescriptor>(),
                Parameters = new List<ParameterDescriptor>(),
            };

            if (actionThrows)
            {
                actionDescriptor.MethodInfo = typeof(ReflectedActionInvokerTest).GetMethod("ThrowingActionMethod");
            }
            else
            {
                actionDescriptor.MethodInfo = typeof(ReflectedActionInvokerTest).GetMethod("ActionMethod");
            }

            var httpContext = new Mock<HttpContext>(MockBehavior.Loose);
            var httpResponse = new Mock<HttpResponse>(MockBehavior.Loose);
            httpContext.SetupGet(c => c.Response).Returns(httpResponse.Object);
            httpResponse.SetupGet(r => r.Body).Returns(new MemoryStream());

            var actionContext = new ActionContext(
                httpContext: httpContext.Object,
                router: null,
                routeValues: null,
                actionDescriptor: actionDescriptor);

            var actionResultFactory = new Mock<IActionResultFactory>(MockBehavior.Strict);
            actionResultFactory
                .Setup(arf => arf.CreateActionResult(It.IsAny<Type>(), It.IsAny<object>(), It.IsAny<ActionContext>()))
                .Returns<Type, object, ActionContext>((t, o, ac) => (IActionResult)o);

            var controllerFactory = new Mock<IControllerFactory>(MockBehavior.Strict);
            controllerFactory.Setup(c => c.CreateController(It.IsAny<ActionContext>())).Returns(this);

            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>(MockBehavior.Strict);
            actionBindingContextProvider
                .Setup(abcp => abcp.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                .Returns(Task.FromResult(new ActionBindingContext(null, null, null, null, null, null)));

            var filterProvider = new Mock<INestedProviderManager<FilterProviderContext>>(MockBehavior.Strict);
            filterProvider
                .Setup(fp => fp.Invoke(It.IsAny<FilterProviderContext>()))
                .Callback<FilterProviderContext>(
                    context => context.Results.AddRange(filters.Select(f => new FilterItem(null, f))));

            var invoker = new ReflectedActionInvoker(
                actionContext,
                actionDescriptor,
                actionResultFactory.Object,
                controllerFactory.Object,
                actionBindingContextProvider.Object,
                filterProvider.Object);

            return invoker;
        }

        public JsonResult ActionMethod()
        {
            return _result;
        }

        public JsonResult ThrowingActionMethod()
        {
            throw _actionException;
        }
    }
}
