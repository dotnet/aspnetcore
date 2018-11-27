// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
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
        public async Task OnResultExecutionAsyn_ExecutesSyncFilters()
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
}
