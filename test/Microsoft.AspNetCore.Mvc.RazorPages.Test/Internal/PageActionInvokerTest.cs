// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionInvokerTest : CommonResourceInvokerTest
    {
        protected override ResourceInvoker CreateInvoker(
            IFilterMetadata[] filters,
            Exception exception = null,
            IActionResult result = null,
            IList<IValueProviderFactory> valueProviderFactories = null)
        {
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                ViewEnginePath = "/Index.cshtml",
                RelativePath = "/Index.cshtml",
                HandlerMethods = new List<HandlerMethodDescriptor>(),
                HandlerTypeInfo = typeof(TestPage).GetTypeInfo(),
                ModelTypeInfo = typeof(TestPage).GetTypeInfo(),
                PageTypeInfo = typeof(TestPage).GetTypeInfo(),
            };

            var handlers = new List<Func<object, object[], Task<IActionResult>>>();
            if (result != null)
            {
                handlers.Add((obj, args) => Task.FromResult(result));
                actionDescriptor.HandlerMethods.Add(new HandlerMethodDescriptor()
                {
                    HttpMethod = "GET",
                    Parameters = new List<HandlerParameterDescriptor>(),
                });
            }
            else if (exception != null)
            {
                handlers.Add((obj, args) => Task.FromException<IActionResult>(exception));
                actionDescriptor.HandlerMethods.Add(new HandlerMethodDescriptor()
                {
                    HttpMethod = "GET",
                    Parameters = new List<HandlerParameterDescriptor>(),
                });
            }

            var executor = new TestPageResultExecutor();
            return CreateInvoker(
                filters,
                actionDescriptor,
                executor,
                handlers: handlers.ToArray());
        }

        private PageActionInvoker CreateInvoker(
            IFilterMetadata[] filters,
            CompiledPageActionDescriptor actionDescriptor,
            PageResultExecutor executor = null,
            PageActionInvokerCacheEntry cacheEntry = null,
            ITempDataDictionaryFactory tempDataFactory = null,
            IList<IValueProviderFactory> valueProviderFactories = null,
            Func<object, object[], Task<IActionResult>>[] handlers = null,
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

            var mvcOptionsAccessor = new TestOptionsManager<MvcOptions>();
            serviceCollection.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            serviceCollection.AddSingleton<IOptions<MvcOptions>>(mvcOptionsAccessor);
            serviceCollection.AddSingleton(new ObjectResultExecutor(
                mvcOptionsAccessor,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

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
            var pageContext = new PageContext(actionContext)
            {
                ActionDescriptor = actionDescriptor,
            };

            var viewDataFactory = ViewDataDictionaryFactory.CreateFactory(actionDescriptor.ModelTypeInfo);
            pageContext.ViewData = viewDataFactory(new EmptyModelMetadataProvider(), pageContext.ModelState);

            if (valueProviderFactories == null)
            {
                valueProviderFactories = new List<IValueProviderFactory>();
            }

            if (logger == null)
            {
                logger = NullLogger.Instance;
            }

            if (tempDataFactory == null)
            {
                tempDataFactory = Mock.Of<ITempDataDictionaryFactory>(m => m.GetTempData(It.IsAny<HttpContext>()) == Mock.Of<ITempDataDictionary>());
            }

            Func<PageContext, ViewContext, object> pageFactory = (context, viewContext) =>
            {
                var instance = (Page)Activator.CreateInstance(actionDescriptor.PageTypeInfo.AsType());
                instance.PageContext = context;
                return instance;
            };

            cacheEntry = new PageActionInvokerCacheEntry(
                actionDescriptor,
                viewDataFactory,
                pageFactory,
                (c, viewContext, page) => { (page as IDisposable)?.Dispose(); },
                _ => Activator.CreateInstance(actionDescriptor.ModelTypeInfo.AsType()),
                (c, model) => { (model as IDisposable)?.Dispose(); },
                null,
                handlers,
                null,
                new FilterItem[0]);

            // Always just select the first one.
            var selector = new Mock<IPageHandlerMethodSelector>();
            selector
                .Setup(s => s.Select(It.IsAny<PageContext>()))
                .Returns<PageContext>(c => c.ActionDescriptor.HandlerMethods.FirstOrDefault());
            
            var invoker = new PageActionInvoker(
                selector.Object,
                diagnosticSource,
                logger,
                pageContext,
                filters,
                valueProviderFactories.ToArray(),
                cacheEntry,
                GetParameterBinder(),
                tempDataFactory,
                new HtmlHelperOptions());
            return invoker;
        }

        private static ParameterBinder GetParameterBinder(
            IModelBinderFactory factory = null,
            IObjectModelValidator validator = null)
        {
            if (validator == null)
            {
                validator = CreateMockValidator();
            }

            if (factory == null)
            {
                factory = TestModelBinderFactory.CreateDefault();
            }

            return new ParameterBinder(
                TestModelMetadataProvider.CreateDefaultProvider(),
                factory,
                validator);
        }

        private static IObjectModelValidator CreateMockValidator()
        {
            var mockValidator = new Mock<IObjectModelValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.Validate(
                    It.IsAny<ActionContext>(),
                    It.IsAny<ValidationStateDictionary>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()));
            return mockValidator.Object;
        }

        private class TestPageResultExecutor : PageResultExecutor
        {
            private readonly Func<PageContext, Task> _executeAction;

            public TestPageResultExecutor()
                : this(null)
            {
            }

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

            public override Task ExecuteAsync(PageContext pageContext, PageResult result)
            {
                return _executeAction?.Invoke(pageContext) ?? Task.CompletedTask;
            }
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
