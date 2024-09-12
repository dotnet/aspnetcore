// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ControllerActionInvokerTest : CommonResourceInvokerTest
{
    #region Diagnostics

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

        actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        actionDescriptor.ControllerTypeInfo = typeof(TestController).GetTypeInfo();

        var listener = new TestDiagnosticListener();

        var routeData = new RouteData();
        routeData.Values.Add("tag", "value");

        var filter = Mock.Of<IFilterMetadata>();
        var invoker = CreateInvoker(
            new[] { filter },
            actionDescriptor,
            controller: new TestController(),
            diagnosticListener: listener,
            routeData: routeData);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.NotNull(listener.BeforeAction?.ActionDescriptor);
        Assert.NotNull(listener.BeforeAction?.HttpContext);

        var routeValues = listener.BeforeAction?.RouteData?.Values;
        Assert.NotNull(routeValues);

        Assert.Single(routeValues);
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

        actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        actionDescriptor.ControllerTypeInfo = typeof(TestController).GetTypeInfo();

        var listener = new TestDiagnosticListener();

        var filter = Mock.Of<IFilterMetadata>();
        var invoker = CreateInvoker(
            new[] { filter },
            actionDescriptor,
            controller: new TestController(),
            diagnosticListener: listener);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.NotNull(listener.AfterAction?.ActionDescriptor);
        Assert.NotNull(listener.AfterAction?.HttpContext);
    }

    [Fact]
    public async Task InvokeAction_ResourceFilter_WritesDiagnostic_Not_ShortCircuited()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor()
        {
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
        };

        actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        actionDescriptor.ControllerTypeInfo = typeof(TestController).GetTypeInfo();

        var listener = new TestDiagnosticListener();
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                await next();
            })
            .Verifiable();

        var invoker = CreateInvoker(
            new IFilterMetadata[] { resourceFilter.Object },
            actionDescriptor,
            controller: new TestController(),
            diagnosticListener: listener);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()), Times.Once);
        Assert.NotNull(listener.BeforeResource?.ActionDescriptor);
        Assert.NotNull(listener.BeforeResource?.ExecutingContext);
        Assert.NotNull(listener.BeforeResource?.Filter);
        Assert.NotNull(listener.AfterResource?.ActionDescriptor);
        Assert.NotNull(listener.AfterResource?.ExecutedContext);
        Assert.NotNull(listener.AfterResource?.Filter);
    }

    [Fact]
    public async Task InvokeAction_ResourceFilter_WritesDiagnostic_ShortCircuited()
    {
        // Arrange
        var actionDescriptor = new ControllerActionDescriptor()
        {
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
        };

        var expected = new Mock<IActionResult>(MockBehavior.Strict);
        expected
            .Setup(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()))
            .Returns(Task.FromResult(true))
            .Verifiable();

        actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        actionDescriptor.ControllerTypeInfo = typeof(TestController).GetTypeInfo();

        var listener = new TestDiagnosticListener();
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>((c, next) =>
            {
                c.Result = expected.Object;
                return Task.FromResult(true);
            })
            .Verifiable();

        var invoker = CreateInvoker(
            new IFilterMetadata[] { resourceFilter.Object },
            actionDescriptor,
            controller: new TestController(),
            diagnosticListener: listener);

        // Act
        await invoker.InvokeAsync();

        // Assert
        resourceFilter.Verify(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()), Times.Once());
        Assert.NotNull(listener.BeforeResource?.ActionDescriptor);
        Assert.NotNull(listener.BeforeResource?.ExecutingContext);
        Assert.NotNull(listener.BeforeResource?.Filter);
        Assert.NotNull(listener.AfterResource?.ActionDescriptor);
        Assert.NotNull(listener.AfterResource?.ExecutedContext);
        Assert.NotNull(listener.AfterResource?.Filter);
    }

    #endregion

    #region Controller Context

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
        var controllerContext = Assert.IsType<ControllerActionInvoker>(invoker).ControllerContext;
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
        var controllerContext = Assert.IsType<ControllerActionInvoker>(invoker).ControllerContext;
        Assert.NotNull(controllerContext);
        Assert.Single(controllerContext.ValueProviderFactories);
        Assert.Same(valueProviderFactory2, controllerContext.ValueProviderFactories[0]);
    }

    #endregion

    #region Action Filters

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

        var invoker = CreateInvoker(filter.Object, result: Result);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
        filter.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

        Assert.Same(Result, result);
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

        var invoker = CreateInvoker(filter.Object, result: Result);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(
            f => f.OnActionExecutionAsync(It.IsAny<ActionExecutingContext>(), It.IsAny<ActionExecutionDelegate>()),
            Times.Once());

        Assert.Same(Result, result);
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

        var invoker = CreateInvoker(filter.Object, exception: Exception);

        // Act
        await invoker.InvokeAsync();

        // Assert
        filter.Verify(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>()), Times.Once());
        filter.Verify(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()), Times.Once());

        Assert.Same(Exception, context.Exception);
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
            exception: Exception);

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

        var invoker = CreateInvoker(new IFilterMetadata[] { resourceFilter.Object, exceptionFilter.Object }, exception: Exception);

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
    public async Task InvokeAction_ExceptionBubbling_AsyncActionFilter_To_ResourceFilter()
    {
        // Arrange
        var resourceFilter = new Mock<IAsyncResourceFilter>(MockBehavior.Strict);
        resourceFilter
            .Setup(f => f.OnResourceExecutionAsync(It.IsAny<ResourceExecutingContext>(), It.IsAny<ResourceExecutionDelegate>()))
            .Returns<ResourceExecutingContext, ResourceExecutionDelegate>(async (c, next) =>
            {
                var context = await next();
                Assert.Same(Exception, context.Exception);
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
            exception: Exception);

        // Act & Assert
        await invoker.InvokeAsync();
    }

    #endregion

    #region Action Method Signatures

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

    [Fact]
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
        Assert.IsType<EmptyResult>(result);
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
        Assert.IsType<ObjectResult>(result);
        Assert.IsType<int>(((ObjectResult)result).Value);
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
    [InlineData(nameof(TestController.AsyncActionMethodWithTestActionResult))]
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

        var controllerContext = new ControllerContext(actionContext)
        {
            ValueProviderFactories = new IValueProviderFactory[0]
        };
        controllerContext.ModelState.MaxAllowedErrors = 200;
        var objectMethodExecutor = ObjectMethodExecutor.Create(
            actionDescriptor.MethodInfo,
            actionDescriptor.ControllerTypeInfo,
            ParameterDefaultValues.GetParameterDefaultValues(actionDescriptor.MethodInfo));

        var controllerMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);

        var cacheEntry = new ControllerActionInvokerCacheEntry(
            new FilterItem[0],
            _ => new TestController(),
            (_, __) => default,
            (_, __, ___) => Task.CompletedTask,
            objectMethodExecutor,
            controllerMethodExecutor,
            controllerMethodExecutor);

        var invoker = new ControllerActionInvoker(
            new NullLoggerFactory().CreateLogger<ControllerActionInvoker>(),
            new DiagnosticListener("Microsoft.AspNetCore"),
            ActionContextAccessor.Null,
            new ActionResultTypeMapper(),
            controllerContext,
            cacheEntry,
            new IFilterMetadata[0]);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.Equal(5, context.Object.Items["Result"]);
    }

    [Fact]
    public async Task InvokeAction_ConvertibleToActionResult()
    {
        // Arrange
        var inputParam = 12;
        var actionParameters = new Dictionary<string, object> { { "input", inputParam }, };
        IActionResult result = null;

        var filter = new Mock<IActionFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
        filter
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Callback<ActionExecutedContext>(c => result = c.Result)
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.ActionReturningConvertibleToActionResult), actionParameters);

        // Act
        await invoker.InvokeAsync();

        // Assert
        var testActionResult = Assert.IsType<TestActionResult>(result);
        Assert.Equal(inputParam, testActionResult.Value);
    }

    [Fact]
    public async Task InvokeAction_AsyncAction_ConvertibleToActionResult()
    {
        // Arrange
        var inputParam = 13;
        var actionParameters = new Dictionary<string, object> { { "input", inputParam }, };
        IActionResult result = null;

        var filter = new Mock<IActionFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
        filter
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Callback<ActionExecutedContext>(c => result = c.Result)
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.ActionReturningConvertibleToActionResultAsync), actionParameters);

        // Act
        await invoker.InvokeAsync();

        // Assert
        var testActionResult = Assert.IsType<TestActionResult>(result);
        Assert.Equal(inputParam, testActionResult.Value);
    }

    [Fact]
    public async Task InvokeAction_ConvertibleToActionResult_AsObject()
    {
        // Arrange
        var actionParameters = new Dictionary<string, object>();
        IActionResult result = null;

        var filter = new Mock<IActionFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
        filter
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Callback<ActionExecutedContext>(c => result = c.Result)
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.ActionReturningConvertibleAsObject), actionParameters);

        // Act
        await invoker.InvokeAsync();

        // Assert
        Assert.IsType<TestActionResult>(result);
    }

    [Fact]
    public async Task InvokeAction_ConvertibleToActionResult_ReturningNull_Throws()
    {
        // Arrange
        var expectedMessage = @"Cannot return null from an action method with a return type of 'Microsoft.AspNetCore.Mvc.Infrastructure.IConvertToActionResult'.";
        var actionParameters = new Dictionary<string, object>();
        IActionResult result = null;

        var filter = new Mock<IActionFilter>(MockBehavior.Strict);
        filter.Setup(f => f.OnActionExecuting(It.IsAny<ActionExecutingContext>())).Verifiable();
        filter
            .Setup(f => f.OnActionExecuted(It.IsAny<ActionExecutedContext>()))
            .Callback<ActionExecutedContext>(c => result = c.Result)
            .Verifiable();

        var invoker = CreateInvoker(new[] { filter.Object }, nameof(TestController.ConvertibleToActionResultReturningNull), actionParameters);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync());
        Assert.Equal(expectedMessage, exception.Message);
    }

    #endregion

    #region Logs

    [Fact]
    public async Task InvokeAsync_Logs()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var actionDescriptor = new ControllerActionDescriptor()
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
            MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod)),
        };

        var invoker = CreateInvoker(
            new IFilterMetadata[0],
            actionDescriptor,
            new TestController(),
            logger: logger);

        // Act
        await invoker.InvokeAsync();

        // Assert
        var messages = testSink.Writes.Select(write => write.State.ToString()).ToList();
        var actionSignature = $"{typeof(IActionResult).FullName} {nameof(TestController.ActionMethod)}()";
        var controllerName = $"{typeof(ControllerActionInvokerTest).FullName}+{nameof(TestController)} ({typeof(ControllerActionInvokerTest).Assembly.GetName().Name})";
        var actionName = $"{typeof(ControllerActionInvokerTest).FullName}+{nameof(TestController)}.{nameof(TestController.ActionMethod)} ({typeof(ControllerActionInvokerTest).Assembly.GetName().Name})";
        var actionResultName = $"{typeof(CommonResourceInvokerTest).FullName}+{nameof(TestResult)}";

        Assert.Collection(
            messages,
            m => Assert.Equal($"Route matched with {{}}. Executing controller action with signature {actionSignature} on controller {controllerName}.", m),
            m => Assert.Equal("Execution plan of authorization filters (in the following order): None", m),
            m => Assert.Equal("Execution plan of resource filters (in the following order): None", m),
            m => Assert.Equal("Execution plan of action filters (in the following order): None", m),
            m => Assert.Equal("Execution plan of exception filters (in the following order): None", m),
            m => Assert.Equal("Execution plan of result filters (in the following order): None", m),
            m => Assert.Equal($"Executing controller factory for controller {controllerName}", m),
            m => Assert.Equal($"Executed controller factory for controller {controllerName}", m),
            m => Assert.Equal($"Executing action method {actionName} - Validation state: Valid", m),
            m => Assert.StartsWith($"Executed action method {actionName}, returned result {actionResultName} in ", m),
            m => Assert.Equal($"Before executing action result {actionResultName}.", m),
            m => Assert.Equal($"After executing action result {actionResultName}.", m),
            m => Assert.StartsWith($"Executed action {actionName} in ", m));
    }

    #endregion

    protected override IActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        Exception exception = null,
        IActionResult result = null,
        IList<IValueProviderFactory> valueProviderFactories = null)
    {
        var actionDescriptor = new ControllerActionDescriptor()
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
        };

        if (result == Result)
        {
            actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        }
        else if (result != null)
        {
            throw new InvalidOperationException($"Unexpected action result {result}.");
        }
        else if (exception == Exception)
        {
            actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ThrowingActionMethod));
        }
        else if (exception != null)
        {
            throw new InvalidOperationException($"Unexpected exception {exception}.");
        }
        else
        {
            actionDescriptor.MethodInfo = typeof(TestController).GetMethod(nameof(TestController.ActionMethod));
        }

        return CreateInvoker(
            filters,
            actionDescriptor,
            new TestController(),
            valueProviderFactories: valueProviderFactories);
    }

    // Used by tests which directly test different types of signatures for controller methods.
    private ControllerActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        string methodName,
        IDictionary<string, object> arguments)
    {
        var actionDescriptor = new ControllerActionDescriptor()
        {
            ControllerTypeInfo = typeof(TestController).GetTypeInfo(),
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
            BoundProperties = new List<ParameterDescriptor>(),
        };

        var method = typeof(TestController).GetTypeInfo().GetMethod(methodName);
        Assert.NotNull(method);
        actionDescriptor.MethodInfo = method;

        foreach (var kvp in arguments)
        {
            actionDescriptor.Parameters.Add(new ControllerParameterDescriptor()
            {
                Name = kvp.Key,
                ParameterInfo = method.GetParameters().Where(p => p.Name == kvp.Key).Single(),
            });
        }

        return CreateInvoker(filters, actionDescriptor, new TestController(), arguments);
    }

    private ControllerActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        ControllerActionDescriptor actionDescriptor,
        object controller,
        IDictionary<string, object> arguments = null,
        IList<IValueProviderFactory> valueProviderFactories = null,
        RouteData routeData = null,
        ILogger logger = null,
        object diagnosticListener = null)
    {
        Assert.NotNull(actionDescriptor.MethodInfo);

        if (arguments == null)
        {
            arguments = new Dictionary<string, object>();
        }

        if (valueProviderFactories == null)
        {
            valueProviderFactories = new List<IValueProviderFactory>();
        }

        if (routeData == null)
        {
            routeData = new RouteData();
        }

        if (logger == null)
        {
            logger = new NullLoggerFactory().CreateLogger<ControllerActionInvoker>();
        }

        var httpContext = new DefaultHttpContext();

        var options = Options.Create(new MvcOptions());

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IOptions<MvcOptions>>(options);
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));

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

        options.Value.OutputFormatters.Add(formatter.Object);

        var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
        if (diagnosticListener != null)
        {
            diagnosticSource.SubscribeWithAdapter(diagnosticListener);
        }

        var objectMethodExecutor = ObjectMethodExecutor.Create(
            actionDescriptor.MethodInfo,
            actionDescriptor.ControllerTypeInfo,
            ParameterDefaultValues.GetParameterDefaultValues(actionDescriptor.MethodInfo));

        var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);

        var cacheEntry = new ControllerActionInvokerCacheEntry(
            new FilterItem[0],
            (c) => controller,
            null,
            (_, __, args) =>
            {
                foreach (var item in arguments)
                {
                    args[item.Key] = item.Value;
                }

                return Task.CompletedTask;
            },
            objectMethodExecutor,
            actionMethodExecutor,
            actionMethodExecutor);

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor);
        var controllerContext = new ControllerContext(actionContext)
        {
            ValueProviderFactories = valueProviderFactories,
        };

        var invoker = new ControllerActionInvoker(
            logger,
            diagnosticSource,
            ActionContextAccessor.Null,
            new ActionResultTypeMapper(),
            controllerContext,
            cacheEntry,
            filters);
        return invoker;
    }

    public sealed class TestController
    {
        public IActionResult ActionMethod()
        {
            return Result;
        }

        public ObjectResult ThrowingActionMethod()
        {
            throw Exception;
        }

        public IActionResult ActionMethodWithDefaultValues(int value = 5)
        {
            return new TestActionResult { Value = value };
        }

        public TestActionResult ActionMethodWithTestActionResult(int value)
        {
            return new TestActionResult { Value = value };
        }

        public async Task<TestActionResult> AsyncActionMethodWithTestActionResult(int value)
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

        public ConvertibleToActionResult ActionReturningConvertibleToActionResult(int input)
            => new ConvertibleToActionResult { Value = input };

        public Task<ConvertibleToActionResult> ActionReturningConvertibleToActionResultAsync(int input)
            => Task.FromResult(new ConvertibleToActionResult { Value = input });

        public object ActionReturningConvertibleAsObject() => new ConvertibleToActionResult();

        public IConvertToActionResult ConvertibleToActionResultReturningNull()
        {
            var mock = new Mock<IConvertToActionResult>();
            mock.Setup(m => m.Convert()).Returns((IActionResult)null);

            return mock.Object;
        }

        public class TaskDerivedType : Task
        {
            public TaskDerivedType()
                : base(() => { })
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

    public sealed class TestActionResult : IActionResult
    {
        public int Value { get; set; }

        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Items["Result"] = Value;
            return Task.FromResult(0);
        }
    }

    private static ObjectMethodExecutor CreateExecutor(ControllerActionDescriptor actionDescriptor)
    {
        return ObjectMethodExecutor.Create(
            actionDescriptor.MethodInfo,
            actionDescriptor.ControllerTypeInfo,
            ParameterDefaultValues.GetParameterDefaultValues(actionDescriptor.MethodInfo));
    }

    public class ConvertibleToActionResult : IConvertToActionResult
    {
        public int Value { get; set; }

        public IActionResult Convert() => new TestActionResult { Value = Value };
    }
}
