// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class MvcCoreLoggerExtensionsTest
    {
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

        public interface IOrderedAuthorizeFilter : IAuthorizationFilter, IAsyncAuthorizationFilter, IOrderedFilter { }

        public interface IOrderedResourceFilter : IResourceFilter, IAsyncResourceFilter, IOrderedFilter { }

        public interface IOrderedActionFilter : IActionFilter, IAsyncActionFilter, IOrderedFilter { }

        public interface IOrderedExceptionFilter : IExceptionFilter, IAsyncExceptionFilter, IOrderedFilter { }

        public interface IOrderedResultFilter : IResultFilter, IAsyncResultFilter, IOrderedFilter { }
    }
}
