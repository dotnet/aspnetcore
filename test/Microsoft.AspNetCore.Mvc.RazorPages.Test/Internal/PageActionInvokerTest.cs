// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerTest
    {
        private readonly DivideByZeroException _pageException = new DivideByZeroException();

        [Fact]
        public async Task InvokeAsync_DoesNotInvokeExceptionFilter_WhenPageDoesNotThrow()
        {
            // Arrange
            var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, pageThrows: false);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Never());
        }

        [Fact]
        public async Task InvokeAsync_DoesNotAsyncInvokeExceptionFilter_WhenPageDoesNotThrow()
        {
            // Arrange
            var filter = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
                .Returns<ExceptionContext>((context) => Task.FromResult(true))
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, pageThrows: false);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Never());
        }

        [Fact]
        public async Task InvokeAsync_InvokesExceptionFilter_WhenPageThrows()
        {
            // Arrange
            Exception exception = null;
            IActionResult pageAction = null;
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
                    pageAction = context.Result;

                    // Handle the exception
                    context.Result = expected.Object;
                })
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter2.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());

            Assert.Same(_pageException, exception);
            Assert.Null(pageAction);
        }

        [Fact]
        public async Task InvokeAsync_InvokesAsyncExceptionFilter_WhenPageThrows()
        {
            // Arrange
            Exception exception = null;
            IActionResult pageAction = null;
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
                    pageAction = context.Result;

                    // Handle the exception
                    context.Result = expected.Object;
                })
                .Returns<ExceptionContext>((context) => Task.FromResult(true))
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            expected.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter2.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());

            Assert.Same(_pageException, exception);
            Assert.Null(pageAction);
        }

        [Fact]
        public async Task InvokeAsync_InvokesExceptionFilter_ShortCircuit_ExceptionNull()
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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter2.Verify(
                f => f.OnException(It.IsAny<ExceptionContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesExceptionFilter_ShortCircuit_ExceptionHandled()
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

            var invoker = CreateInvoker(new[] { filter1.Object, filter2.Object }, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter2.Verify(
                f => f.OnException(It.IsAny<ExceptionContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionNull()
        {
            // Arrange
            var filter1 = new Mock<IExceptionFilter>(MockBehavior.Strict);
            var filter2 = new Mock<IAsyncExceptionFilter>(MockBehavior.Strict);

            filter2
                .Setup(f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()))
                .Callback<ExceptionContext>(context =>
                {
                    filter2.ToString();
                    context.Exception = null;
                })
                .Returns<ExceptionContext>((context) => Task.FromResult(true))
                .Verifiable();

            var filterMetadata = new IFilterMetadata[] { filter1.Object, filter2.Object };
            var invoker = CreateInvoker(filterMetadata, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter2.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesAsyncExceptionFilter_ShortCircuit_ExceptionHandled()
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

            var invoker = CreateInvoker(new IFilterMetadata[] { filter1.Object, filter2.Object }, pageThrows: true);

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter2.Verify(
                f => f.OnExceptionAsync(It.IsAny<ExceptionContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesExceptionFilter_UnhandledExceptionIsThrown()
        {
            // Arrange
            var filter = new Mock<IExceptionFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnException(It.IsAny<ExceptionContext>()))
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object }, pageThrows: true);

            // Act
            await Assert.ThrowsAsync(_pageException.GetType(), invoker.InvokeAsync);

            // Assert
            filter.Verify(f => f.OnException(It.IsAny<ExceptionContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesAuthorizationFilter()
        {
            // Arrange
            var filter = new Mock<IAuthorizationFilter>(MockBehavior.Strict);
            filter.Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>())).Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesAsyncAuthorizationFilter()
        {
            // Arrange
            var filter = new Mock<IAsyncAuthorizationFilter>(MockBehavior.Strict);
            filter
                .Setup(f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()))
                .Returns<AuthorizationFilterContext>(context => Task.FromResult(true))
                .Verifiable();

            var invoker = CreateInvoker(new[] { filter.Object });

            // Act
            await invoker.InvokeAsync();

            // Assert
            filter.Verify(
                f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()),
                Times.Once());
        }

        [Fact]
        public async Task InvokeAsync_InvokesAuthorizationFilter_ShortCircuit()
        {
            // Arrange
            var createCalled = false;
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
            var actionDescriptor = new CompiledPageActionDescriptor();
            var cacheEntry = new PageActionInvokerCacheEntry(
                actionDescriptor,
                (context) => createCalled = true,
                null,
                (context) => null,
                null,
                null,
                null,
                new FilterItem[0]);
            var invoker = CreateInvoker(
                new[] { filter1.Object, filter2.Object, filter3.Object },
                actionDescriptor,
                cacheEntry: cacheEntry);

            // Act
            await invoker.InvokeAsync();

            // Assert
            challenge.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter1.Verify(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()), Times.Once());
            Assert.False(createCalled);
        }

        [Fact]
        public async Task InvokeAsync_InvokesAsyncAuthorizationFilter_ShortCircuit()
        {
            // Arrange
            var createCalled = false;
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

            var actionDescriptor = new CompiledPageActionDescriptor();
            var cacheEntry = new PageActionInvokerCacheEntry(
                actionDescriptor,
                (context) => createCalled = true,
                null,
                (context) => null,
                null,
                null,
                null,
                new FilterItem[0]);
            var invoker = CreateInvoker(
                new IFilterMetadata[] { filter1.Object, filter2.Object, filter3.Object },
                actionDescriptor,
                cacheEntry: cacheEntry);

            // Act
            await invoker.InvokeAsync();

            // Assert
            challenge.Verify(r => r.ExecuteResultAsync(It.IsAny<ActionContext>()), Times.Once());
            filter1.Verify(
                f => f.OnAuthorizationAsync(It.IsAny<AuthorizationFilterContext>()),
                Times.Once());

            Assert.False(createCalled);
        }

        [Fact]
        public async Task InvokeAsync_ExceptionInAuthorizationFilter_CannotBeHandledByOtherFilters()
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
        public async Task InvokeAsync_InvokesAuthorizationFilter_ChallengeNotSeenByResultFilters()
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

        private PageActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            bool pageThrows = false,
            int maxAllowedErrorsInModelState = 200,
            List<IValueProviderFactory> valueProviderFactories = null)
        {
            Func<PageContext, Task> executeAction;
            if (pageThrows)
            {
                executeAction = _ => { throw _pageException; };
            }
            else
            {
                executeAction = context => context.HttpContext.Response.WriteAsync("Hello");
            }
            var executor = new TestPageResultExecutor(executeAction);
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                ViewEnginePath = "/Index.cshtml",
                RelativePath = "/Index.cshtml",
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };

            return CreateInvoker(
                filters,
                actionDescriptor,
                executor);
        }

        private PageActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            CompiledPageActionDescriptor actionDescriptor,
            PageResultExecutor executor = null,
            IPageHandlerMethodSelector selector = null,
            PageActionInvokerCacheEntry cacheEntry = null,
            int maxAllowedErrorsInModelState = 200,
            List<IValueProviderFactory> valueProviderFactories = null,
            RouteData routeData = null,
            ILogger logger = null)
        {
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");

            var httpContext = new DefaultHttpContext();
            var serviceCollection = new ServiceCollection();
            if (executor == null)
            {
                executor = new PageResultExecutor(
                    Mock.Of<IHttpResponseStreamWriterFactory>(),
                    Mock.Of<ICompositeViewEngine>(),
                    Mock.Of<IRazorViewEngine>(),
                    Mock.Of<IRazorPageActivator>(),
                    diagnosticSource,
                    HtmlEncoder.Default);
            }

            serviceCollection.AddSingleton(executor ?? executor);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            if (routeData == null)
            {
                routeData = new RouteData();
            }

            var actionContext = new ActionContext(
                httpContext: httpContext,
                routeData: routeData,
                actionDescriptor: actionDescriptor);
            var pageContext = new PageContext(
                actionContext,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                Mock.Of<ITempDataDictionary>(),
                new HtmlHelperOptions())
            {
                ActionDescriptor = actionDescriptor
            };

            if (selector == null)
            {
                selector = Mock.Of<IPageHandlerMethodSelector>();
            }

            if (valueProviderFactories == null)
            {
                valueProviderFactories = new List<IValueProviderFactory>();
            }

            if (logger == null)
            {
                logger = NullLogger.Instance;
            }

            Func<PageContext, object> pageFactory = (context) =>
            {
                var instance = (Page)Activator.CreateInstance(actionDescriptor.PageTypeInfo.AsType());
                instance.PageContext = context;
                return instance;
            };

            cacheEntry = new PageActionInvokerCacheEntry(
                actionDescriptor,
                pageFactory,
                (c, page) => { (page as IDisposable)?.Dispose(); },
                _ => Activator.CreateInstance(actionDescriptor.ModelTypeInfo.AsType()),
                (c, model) => { (model as IDisposable)?.Dispose(); },
                null,
                null,
                new FilterItem[0]);

            var invoker = new PageActionInvoker(
                selector,
                diagnosticSource,
                logger,
                pageContext,
                filters,
                valueProviderFactories.AsReadOnly(),
                cacheEntry);
            return invoker;
        }

        private class TestPageResultExecutor : PageResultExecutor
        {
            private readonly Func<PageContext, Task> _executeAction;

            public TestPageResultExecutor(Func<PageContext, Task> executeAction)
                : base(
                    Mock.Of<IHttpResponseStreamWriterFactory>(),
                    Mock.Of<ICompositeViewEngine>(),
                    Mock.Of<IRazorViewEngine>(),
                    Mock.Of<IRazorPageActivator>(),
                    new DiagnosticListener("Microsoft.AspNetCore"),
                    HtmlEncoder.Default)
            {
                _executeAction = executeAction;
            }

            public override Task ExecuteAsync(PageContext pageContext, PageViewResult result)
                => _executeAction(pageContext);
        }

        private class TestPage : Page
        {
            public override Task ExecuteAsync()
            {
                throw new NotImplementedException();
            }
        }
    }
}
