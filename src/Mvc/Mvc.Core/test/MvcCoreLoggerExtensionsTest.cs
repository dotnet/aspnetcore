// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class MvcCoreLoggerExtensionsTest
{
    public static object[][] RouteValuesTestData { get; } = new object[][]
    {
        new object[]{ "{}" },
        new object[]{ "{foo = \"bar\"}", new KeyValuePair<string, string>("foo", "bar") },
        new object[]{ "{foo = \"bar\", other = \"value\"}",
            new KeyValuePair<string, string>("foo", "bar"),
            new KeyValuePair<string, string>("other", "value") },
    };

    public static object[][] PageRouteValuesTestData { get; } = new object[][]
    {
        new object[]{ "{page = \"bar\"}", new KeyValuePair<string, string>("page", "bar") },
        new object[]{ "{page = \"bar\", other = \"value\"}",
            new KeyValuePair<string, string>("page", "bar"),
            new KeyValuePair<string, string>("other", "value") },
    };

    [Theory]
    [MemberData(nameof(RouteValuesTestData))]
    public void ExecutingAction_ForControllerAction_WithGivenRouteValues_LogsActionAndRouteData(string expectedRouteValuesLogMessage, params KeyValuePair<string, string>[] routeValues)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var action = new Controllers.ControllerActionDescriptor
        {
            // Using a generic type to verify the use of a clean name
            ControllerTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo(),
            MethodInfo = typeof(object).GetMethod(nameof(ToString)),
        };

        foreach (var routeValue in routeValues)
        {
            action.RouteValues.Add(routeValue);
        }

        // Act
        ResourceInvoker.Log.ExecutingAction(logger, action);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            $"Route matched with {expectedRouteValuesLogMessage}. " +
            "Executing controller action with signature System.String ToString() on controller System.ValueTuple<int, string> (System.Private.CoreLib).",
            write.State.ToString());
    }

    [Theory]
    [MemberData(nameof(RouteValuesTestData))]
    public void ExecutingAction_ForAction_WithGivenRouteValues_LogsActionAndRouteData(string expectedRouteValuesLogMessage, params KeyValuePair<string, string>[] routeValues)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var action = new ActionDescriptor
        {
            DisplayName = "foobar",
        };

        foreach (var routeValue in routeValues)
        {
            action.RouteValues.Add(routeValue);
        }

        // Act
        ResourceInvoker.Log.ExecutingAction(logger, action);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            $"Route matched with {expectedRouteValuesLogMessage}. Executing action {action.DisplayName}",
            write.State.ToString());
    }

    [Theory]
    [MemberData(nameof(PageRouteValuesTestData))]
    public void ExecutingAction_ForPage_WithGivenRouteValues_LogsPageAndRouteData(string expectedRouteValuesLogMessage, params KeyValuePair<string, string>[] routeValues)
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var action = new ActionDescriptor
        {
            DisplayName = "/Pages/Foo",
        };

        foreach (var routeValue in routeValues)
        {
            action.RouteValues.Add(routeValue);
        }

        // Act
        ResourceInvoker.Log.ExecutingAction(logger, action);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            $"Route matched with {expectedRouteValuesLogMessage}. Executing page {action.DisplayName}",
            write.State.ToString());
    }

    [Fact]
    public void LogsFilters_OnlyWhenLogger_IsEnabled()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                authFilter,
                asyncAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                resultFilter,
                asyncResultFilter,
                resourceFilter,
                asyncResourceFilter
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: false);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.AuthorizationFiltersExecutionPlan(filters);
        logger.ResourceFiltersExecutionPlan(filters);
        logger.ActionFiltersExecutionPlan(filters);
        logger.ExceptionFiltersExecutionPlan(filters);
        logger.ResultFiltersExecutionPlan(filters);

        // Assert
        Assert.Empty(testSink.Writes);
    }

    [Fact]
    public void LogsListOfAuthorizationFilters()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var orderedAuthFilterMock = new Mock<IOrderedAuthorizeFilter>();
        orderedAuthFilterMock.SetupGet(f => f.Order).Returns(-100);
        var orderedAuthFilter = orderedAuthFilterMock.Object;
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                authFilter,
                asyncAuthFilter,
                orderedAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                resultFilter,
                asyncResultFilter,
                resourceFilter,
                asyncResourceFilter
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.AuthorizationFiltersExecutionPlan(filters);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Execution plan of authorization filters (in the following order): " +
            $"{authFilter.GetType()}, {asyncAuthFilter.GetType()}, {orderedAuthFilter.GetType()} (Order: -100)",
            write.State.ToString());
    }

    [Fact]
    public void LogsListOfResourceFilters()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var orderedResourceFilterMock = new Mock<IOrderedResourceFilter>();
        orderedResourceFilterMock.SetupGet(f => f.Order).Returns(-100);
        var orderedResourceFilter = orderedResourceFilterMock.Object;
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                authFilter,
                asyncAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                resultFilter,
                asyncResultFilter,
                resourceFilter,
                asyncResourceFilter,
                orderedResourceFilter,
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.ResourceFiltersExecutionPlan(filters);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Execution plan of resource filters (in the following order): " +
            $"{resourceFilter.GetType()}, {asyncResourceFilter.GetType()}, {orderedResourceFilter.GetType()} (Order: -100)",
            write.State.ToString());
    }

    [Fact]
    public void LogsListOfActionFilters()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var orderedActionFilterMock = new Mock<IOrderedActionFilter>();
        orderedActionFilterMock.SetupGet(f => f.Order).Returns(-100);
        var orderedActionFilter = orderedActionFilterMock.Object;
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                orderedActionFilter,
                authFilter,
                asyncAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                resultFilter,
                asyncResultFilter,
                resourceFilter,
                asyncResourceFilter,
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.ActionFiltersExecutionPlan(filters);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Execution plan of action filters (in the following order): " +
            $"{actionFilter.GetType()}, {asyncActionFilter.GetType()}, {orderedActionFilter.GetType()} (Order: -100)",
            write.State.ToString());
    }

    [Fact]
    public void LogsListOfExceptionFilters()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var orderedExceptionFilterMock = new Mock<IOrderedExceptionFilter>();
        orderedExceptionFilterMock.SetupGet(f => f.Order).Returns(-100);
        var orderedExceptionFilter = orderedExceptionFilterMock.Object;
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                authFilter,
                asyncAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                orderedExceptionFilter,
                resultFilter,
                asyncResultFilter,
                resourceFilter,
                asyncResourceFilter,
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.ExceptionFiltersExecutionPlan(filters);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Execution plan of exception filters (in the following order): " +
            $"{exceptionFilter.GetType()}, {asyncExceptionFilter.GetType()}, {orderedExceptionFilter.GetType()} (Order: -100)",
            write.State.ToString());
    }

    [Fact]
    public void LogsListOfResultFilters()
    {
        // Arrange
        var authFilter = Mock.Of<IAuthorizationFilter>();
        var asyncAuthFilter = Mock.Of<IAsyncAuthorizationFilter>();
        var actionFilter = Mock.Of<IActionFilter>();
        var asyncActionFilter = Mock.Of<IAsyncActionFilter>();
        var exceptionFilter = Mock.Of<IExceptionFilter>();
        var asyncExceptionFilter = Mock.Of<IAsyncExceptionFilter>();
        var orderedResultFilterMock = new Mock<IOrderedResultFilter>();
        orderedResultFilterMock.SetupGet(f => f.Order).Returns(-100);
        var orderedResultFilter = orderedResultFilterMock.Object;
        var resultFilter = Mock.Of<IResultFilter>();
        var asyncResultFilter = Mock.Of<IAsyncResultFilter>();
        var resourceFilter = Mock.Of<IResourceFilter>();
        var asyncResourceFilter = Mock.Of<IAsyncResourceFilter>();
        var filters = new IFilterMetadata[]
        {
                actionFilter,
                asyncActionFilter,
                authFilter,
                asyncAuthFilter,
                exceptionFilter,
                asyncExceptionFilter,
                resultFilter,
                asyncResultFilter,
                orderedResultFilter,
                resourceFilter,
                asyncResourceFilter,
        };
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        // Act
        logger.ResultFiltersExecutionPlan(filters);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Execution plan of result filters (in the following order): " +
            $"{resultFilter.GetType()}, {asyncResultFilter.GetType()}, {orderedResultFilter.GetType()} (Order: -100)",
            write.State.ToString());
    }

    [Fact]
    public void NoFormatter_LogsListOfContentTypes()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var mediaTypes = new MediaTypeCollection
            {
                "application/problem+json",
                "application/problem+xml",
            };

        var httpContext = Mock.Of<HttpContext>();
        var context = new Mock<OutputFormatterCanWriteContext>(httpContext);

        context.SetupGet(x => x.ContentType).Returns("application/json");

        // Act
        ObjectResultExecutor.Log.NoFormatter(logger, context.Object, mediaTypes);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "No output formatter was found for content types " +
            "'application/problem+json, application/problem+xml, application/json'" +
            " to write the response.",
            write.State.ToString());
    }

    [Fact]
    public void ExecutingControllerFactory_LogsControllerName()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var context = new ControllerContext
        {
            ActionDescriptor = new Controllers.ControllerActionDescriptor
            {
                // Using a generic type to verify the use of a clean name
                ControllerTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
            }
        };

        // Act
        ControllerActionInvoker.Log.ExecutingControllerFactory(logger, context);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Executing controller factory for controller " +
            "System.ValueTuple<int, string> (System.Private.CoreLib)",
            write.State.ToString());
    }

    [Fact]
    public void ExecutedControllerFactory_LogsControllerName()
    {
        // Arrange
        var testSink = new TestSink();
        var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
        var logger = loggerFactory.CreateLogger("test");

        var context = new ControllerContext
        {
            ActionDescriptor = new Controllers.ControllerActionDescriptor
            {
                // Using a generic type to verify the use of a clean name
                ControllerTypeInfo = typeof(ValueTuple<int, string>).GetTypeInfo()
            }
        };

        // Act
        ControllerActionInvoker.Log.ExecutedControllerFactory(logger, context);

        // Assert
        var write = Assert.Single(testSink.Writes);
        Assert.Equal(
            "Executed controller factory for controller " +
            "System.ValueTuple<int, string> (System.Private.CoreLib)",
            write.State.ToString());
    }

    public interface IOrderedAuthorizeFilter : IAuthorizationFilter, IAsyncAuthorizationFilter, IOrderedFilter { }

    public interface IOrderedResourceFilter : IResourceFilter, IAsyncResourceFilter, IOrderedFilter { }

    public interface IOrderedActionFilter : IActionFilter, IAsyncActionFilter, IOrderedFilter { }

    public interface IOrderedExceptionFilter : IExceptionFilter, IAsyncExceptionFilter, IOrderedFilter { }

    public interface IOrderedResultFilter : IResultFilter, IAsyncResultFilter, IOrderedFilter { }
}
