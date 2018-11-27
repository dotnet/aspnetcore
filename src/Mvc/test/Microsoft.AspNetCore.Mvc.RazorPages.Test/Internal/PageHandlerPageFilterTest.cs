// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageHandlerPageFilterTest
    {
        [Fact]
        public async Task OnPageHandlerExecutionAsync_ExecutesAsyncFilters()
        {
            // Arrange
            var pageContext = new PageContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new PageActionDescriptor(),
                new ModelStateDictionary()));
            var model = new Mock<PageModel>();

            var pageHandlerExecutingContext = new PageHandlerExecutingContext(
               pageContext,
               Array.Empty<IFilterMetadata>(),
               new HandlerMethodDescriptor(),
               new Dictionary<string, object>(),
               model.Object);
            var pageHandlerExecutedContext = new PageHandlerExecutedContext(
              pageContext,
              Array.Empty<IFilterMetadata>(),
              new HandlerMethodDescriptor(),
              model.Object);
            PageHandlerExecutionDelegate next = () => Task.FromResult(pageHandlerExecutedContext);

            var modelAsFilter = model.As<IAsyncPageFilter>();
            modelAsFilter
                .Setup(f => f.OnPageHandlerExecutionAsync(pageHandlerExecutingContext, next))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var pageHandlerPageFilter = new PageHandlerPageFilter();

            // Act
            await pageHandlerPageFilter.OnPageHandlerExecutionAsync(pageHandlerExecutingContext, next);

            // Assert
            modelAsFilter.Verify();
        }

        [Fact]
        public async Task OnPageHandlerExecutionAsync_ExecutesSyncFilters()
        {
            // Arrange
            var pageContext = new PageContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new PageActionDescriptor(),
                new ModelStateDictionary()));
            var model = new Mock<object>();

            var modelAsFilter = model.As<IPageFilter>();
            modelAsFilter
                .Setup(f => f.OnPageHandlerExecuting(It.IsAny<PageHandlerExecutingContext>()))
                .Verifiable();

            modelAsFilter
                .Setup(f => f.OnPageHandlerExecuted(It.IsAny<PageHandlerExecutedContext>()))
                .Verifiable();

            var pageHandlerExecutingContext = new PageHandlerExecutingContext(
               pageContext,
               Array.Empty<IFilterMetadata>(),
               new HandlerMethodDescriptor(),
               new Dictionary<string, object>(),
               model.Object);
            var pageHandlerExecutedContext = new PageHandlerExecutedContext(
              pageContext,
              Array.Empty<IFilterMetadata>(),
              new HandlerMethodDescriptor(),
              model.Object);
            PageHandlerExecutionDelegate next = () => Task.FromResult(pageHandlerExecutedContext);

            var pageHandlerPageFilter = new PageHandlerPageFilter();

            // Act
            await pageHandlerPageFilter.OnPageHandlerExecutionAsync(pageHandlerExecutingContext, next);

            // Assert
            modelAsFilter.Verify();
        }

        [Fact]
        public async Task OnPageHandlerExecutionAsync_DoesNotInvokeHandlerExecuted_IfResultIsSet()
        {
            // Arrange
            var pageContext = new PageContext(new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new PageActionDescriptor(),
                new ModelStateDictionary()));
            var model = new Mock<object>();

            var modelAsFilter = model.As<IPageFilter>();
            modelAsFilter
                .Setup(f => f.OnPageHandlerExecuting(It.IsAny<PageHandlerExecutingContext>()))
                .Callback((PageHandlerExecutingContext context) => context.Result = new PageResult())
                .Verifiable();

            modelAsFilter
                .Setup(f => f.OnPageHandlerExecuted(It.IsAny<PageHandlerExecutedContext>()))
                .Throws(new Exception("Shouldn't be called"));

            var pageHandlerExecutingContext = new PageHandlerExecutingContext(
               pageContext,
               Array.Empty<IFilterMetadata>(),
               new HandlerMethodDescriptor(),
               new Dictionary<string, object>(),
               model.Object);
            var pageHandlerExecutedContext = new PageHandlerExecutedContext(
              pageContext,
              Array.Empty<IFilterMetadata>(),
              new HandlerMethodDescriptor(),
              model.Object);
            PageHandlerExecutionDelegate next = () => Task.FromResult(pageHandlerExecutedContext);

            var pageHandlerPageFilter = new PageHandlerPageFilter();

            // Act
            await pageHandlerPageFilter.OnPageHandlerExecutionAsync(pageHandlerExecutingContext, next);

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

            var pageHandlerExecutingContext = new PageHandlerExecutingContext(
               pageContext,
               Array.Empty<IFilterMetadata>(),
               new HandlerMethodDescriptor(),
               new Dictionary<string, object>(),
               model);
            var pageHandlerExecutedContext = new PageHandlerExecutedContext(
              pageContext,
              Array.Empty<IFilterMetadata>(),
              new HandlerMethodDescriptor(),
              model);
            var invoked = false;
            PageHandlerExecutionDelegate next = () =>
            {
                invoked = true;
                return Task.FromResult(pageHandlerExecutedContext);
            };

            var pageHandlerPageFilter = new PageHandlerPageFilter();

            // Act
            await pageHandlerPageFilter.OnPageHandlerExecutionAsync(pageHandlerExecutingContext, next);

            // Assert
            Assert.True(invoked);
        }
    }
}
