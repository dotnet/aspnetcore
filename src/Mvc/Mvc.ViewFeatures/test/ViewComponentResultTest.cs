// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class ViewComponentResultTest
    {
        private readonly ITempDataDictionary _tempDataDictionary =
            new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        [Fact]
        public void Model_ExposesViewDataModel()
        {
            // Arrange
            var customModel = new object();
            var viewResult = new ViewComponentResult
            {
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider())
                {
                    Model = customModel
                },
            };

            // Act & Assert
            Assert.Same(customModel, viewResult.Model);
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfServicesNotRegistered()
        {
            // Arrange
            var actionContext = new ActionContext(new DefaultHttpContext() { RequestServices = Mock.Of<IServiceProvider>(), }, new RouteData(), new ActionDescriptor());
            var expected =
                $"Unable to find the required services. Please add all the required services by calling " +
                $"'IServiceCollection.AddControllersWithViews()' inside the call to 'ConfigureServices(...)' " +
                $"in the application startup code.";

            var viewResult = new ViewComponentResult();

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => viewResult.ExecuteResultAsync(actionContext));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public async Task ExecuteAsync_ViewComponentResult_AllowsNullViewDataAndTempData()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult
            {
                Arguments = new { name = "World!" },
                ViewData = null,
                TempData = null,
                ViewComponentName = "Text"
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);
            // No assert, just confirm it didn't throw
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfNameOrTypeIsNotSet()
        {
            // Arrange
            var expected =
                "Either the 'ViewComponentName' or 'ViewComponentType' " +
                "property must be set in order to invoke a view component.";

            var actionContext = CreateActionContext();

            var viewComponentResult = new ViewComponentResult
            {
                TempData = _tempDataDictionary,
            };

            // Act and Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewComponentResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewComponentCouldNotBeFound_ByName()
        {
            // Arrange
            var expected = "A view component named 'Text' could not be found. A view component must be " +
                "a public non-abstract class, not contain any generic parameters, and either be decorated " +
                "with 'ViewComponentAttribute' or have a class name ending with the 'ViewComponent' suffix. " +
                "A view component must not be decorated with 'NonViewComponentAttribute'.";

            var actionContext = CreateActionContext();

            var viewComponentResult = new ViewComponentResult
            {
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act and Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewComponentResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task ExecuteResultAsync_Throws_IfViewComponentCouldNotBeFound_ByType()
        {
            // Arrange
            var expected = $"A view component named '{typeof(TextViewComponent).FullName}' could not be found. " +
                "A view component must be a public non-abstract class, not contain any generic parameters, and either be decorated " +
                "with 'ViewComponentAttribute' or have a class name ending with the 'ViewComponent' suffix. " +
                "A view component must not be decorated with 'NonViewComponentAttribute'.";

            var actionContext = CreateActionContext();
            var services = CreateServices(diagnosticListener: null, context: actionContext.HttpContext);
            services.AddSingleton<IViewComponentSelector>();


            var viewComponentResult = new ViewComponentResult
            {
                ViewComponentType = typeof(TextViewComponent),
                TempData = _tempDataDictionary,
            };

            // Act and Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewComponentResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesSyncViewComponent()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesDictionaryArguments()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new Dictionary<string, object> { ["name"] = "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesAsyncViewComponent()
        {
            // Arrange
            var methodInfo = typeof(AsyncTextViewComponent).GetMethod(nameof(AsyncTextViewComponent.InvokeAsync));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.AsyncText",
                ShortName = "AsyncText",
                TypeInfo = typeof(AsyncTextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "AsyncText",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello-Async, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesViewComponent_AndWritesDiagnosticListener()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var adapter = new TestDiagnosticListener();

            var actionContext = CreateActionContext(adapter, descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);

            Assert.NotNull(adapter.BeforeViewComponent?.ActionDescriptor);
            Assert.NotNull(adapter.BeforeViewComponent?.ViewComponentContext);
            Assert.NotNull(adapter.BeforeViewComponent?.ViewComponent);
            Assert.NotNull(adapter.AfterViewComponent?.ActionDescriptor);
            Assert.NotNull(adapter.AfterViewComponent?.ViewComponentContext);
            Assert.NotNull(adapter.AfterViewComponent?.ViewComponentResult);
            Assert.NotNull(adapter.AfterViewComponent?.ViewComponent);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesViewComponent_ByShortName()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesViewComponent_ByFullName()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Full.Name.Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_ExecutesViewComponent_ByType()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentType = typeof(TextViewComponent),
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal("Hello, World!", body);
        }

        [Fact]
        public async Task ExecuteResultAsync_WithCustomViewComponentHelper()
        {
            // Arrange
            var expected = "Hello from custom helper";
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };
            var result = Task.FromResult<IHtmlContent>(new HtmlContentBuilder().AppendHtml(expected));

            var helper = Mock.Of<IViewComponentHelper>(h => h.InvokeAsync(It.IsAny<Type>(), It.IsAny<object>()) == result);

            var httpContext = new DefaultHttpContext();
            var services = CreateServices(diagnosticListener: null, httpContext, new[] { descriptor });
            services.AddSingleton<IViewComponentHelper>(helper);

            httpContext.RequestServices = services.BuildServiceProvider();
            httpContext.Response.Body = new MemoryStream();

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentType = typeof(TextViewComponent),
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task ExecuteResultAsync_WithCustomViewComponentHelper_ForLargeText()
        {
            // Arrange
            var expected = new string('a', 64 * 1024 * 1024);
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };
            var result = Task.FromResult<IHtmlContent>(new HtmlContentBuilder().AppendHtml(expected));

            var helper = Mock.Of<IViewComponentHelper>(h => h.InvokeAsync(It.IsAny<Type>(), It.IsAny<object>()) == result);

            var httpContext = new DefaultHttpContext();
            var services = CreateServices(diagnosticListener: null, httpContext, new[] { descriptor });
            services.AddSingleton<IViewComponentHelper>(helper);

            httpContext.RequestServices = services.BuildServiceProvider();
            httpContext.Response.Body = new MemoryStream();

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentType = typeof(TextViewComponent),
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var body = ReadBody(actionContext.HttpContext.Response);
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsStatusCode()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentType = typeof(TextViewComponent),
                StatusCode = 404,
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(404, actionContext.HttpContext.Response.StatusCode);
        }

        public static TheoryData<string, string> ViewComponentResultContentTypeData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    {
                        null,
                        "text/html; charset=utf-8"
                    },
                    {
                        "text/foo",
                        "text/foo"
                    },
                    {
                        "text/foo;p1=p1-value",
                        "text/foo; p1=p1-value"
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Encoding = Encoding.ASCII }.ToString(),
                        "text/foo; charset=us-ascii"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewComponentResultContentTypeData))]
        public async Task ViewComponentResult_SetsContentTypeHeader(
            string contentType,
            string expectedContentType)
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var contentTypeBeforeViewResultExecution = contentType?.ToString();

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                ContentType = contentType,
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            var resultContentType = actionContext.HttpContext.Response.ContentType;
            MediaTypeAssert.Equal(expectedContentType, resultContentType);

            // Check if the original instance provided by the user has not changed.
            // Since we do not have access to the new instance created within the view executor,
            // check if at least the content is the same.
            var contentTypeAfterViewResultExecution = contentType?.ToString();
            MediaTypeAssert.Equal(contentTypeBeforeViewResultExecution, contentTypeAfterViewResultExecution);
        }

        [Fact]
        public async Task ViewComponentResult_SetsContentTypeHeader_OverrideResponseContentType()
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            var expectedContentType = "text/html; charset=utf-8";
            actionContext.HttpContext.Response.ContentType = "application/x-will-be-overridden";

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                ContentType = new MediaTypeHeaderValue("text/html") { Encoding = Encoding.UTF8 }.ToString(),
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.ContentType);
        }

        [Theory]
        [InlineData("text/foo", "text/foo; charset=utf-8")]
        [InlineData("text/foo; p1=p1-value", "text/foo; p1=p1-value; charset=utf-8")]
        [InlineData("text/foo; p1=p1-value; charset=us-ascii", "text/foo; p1=p1-value; charset=us-ascii")]
        public async Task ViewComponentResult_NoContentTypeSet_PreservesResponseContentType(
            string responseContentType,
            string expectedContentType)
        {
            // Arrange
            var methodInfo = typeof(TextViewComponent).GetMethod(nameof(TextViewComponent.Invoke));
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                TypeInfo = typeof(TextViewComponent).GetTypeInfo(),
                MethodInfo = methodInfo,
                Parameters = methodInfo.GetParameters(),
            };

            var actionContext = CreateActionContext(descriptor);

            actionContext.HttpContext.Response.ContentType = expectedContentType;

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new { name = "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.ContentType);
        }

        private IServiceCollection CreateServices(
            object diagnosticListener,
            HttpContext context,
            params ViewComponentDescriptor[] descriptors)
        {
            var httpContext = new DefaultHttpContext();
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");
            if (diagnosticListener != null)
            {
                diagnosticSource.SubscribeWithAdapter(diagnosticListener);
            }

            var services = new ServiceCollection();
            services.AddSingleton<DiagnosticListener>(diagnosticSource);
            services.AddSingleton<ViewComponentInvokerCache>();
            services.AddSingleton(Options.Create(new MvcViewOptions()));
            services.AddTransient<IViewComponentHelper, DefaultViewComponentHelper>();
            services.AddSingleton<IViewComponentSelector, DefaultViewComponentSelector>();
            services.AddSingleton<IViewComponentDescriptorCollectionProvider, DefaultViewComponentDescriptorCollectionProvider>();
            services.AddSingleton<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            services.AddSingleton<ITypeActivatorCache, TypeActivatorCache>();
            services.AddSingleton<IViewComponentActivator, DefaultViewComponentActivator>();
            services.AddSingleton<IViewComponentFactory, DefaultViewComponentFactory>();
            services.AddSingleton<IViewComponentDescriptorProvider>(new FixedSetViewComponentDescriptorProvider(descriptors));
            services.AddSingleton<IModelMetadataProvider, EmptyModelMetadataProvider>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
            services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
            services.AddSingleton<TempDataSerializer, DefaultTempDataSerializer>();
            services.AddSingleton<HtmlEncoder, HtmlTestEncoder>();
            services.AddSingleton<IViewBufferScope, TestViewBufferScope>();
            services.AddSingleton<IActionResultExecutor<ViewComponentResult>, ViewComponentResultExecutor>();
            services.AddSingleton<IHttpResponseStreamWriterFactory, TestHttpResponseStreamWriterFactory>();

            return services;
        }

        private HttpContext CreateHttpContext(object diagnosticListener, params ViewComponentDescriptor[] descriptors)
        {
            var httpContext = new DefaultHttpContext();
            var services = CreateServices(diagnosticListener, httpContext, descriptors);

            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private ActionContext CreateActionContext(object diagnosticListener, params ViewComponentDescriptor[] descriptors)
        {
            return new ActionContext(CreateHttpContext(diagnosticListener, descriptors), new RouteData(), new ActionDescriptor());
        }

        private ActionContext CreateActionContext(params ViewComponentDescriptor[] descriptors)
        {
            return CreateActionContext(null, descriptors);
        }

        private class FixedSetViewComponentDescriptorProvider : IViewComponentDescriptorProvider
        {
            private readonly ViewComponentDescriptor[] _descriptors;

            public FixedSetViewComponentDescriptorProvider(params ViewComponentDescriptor[] descriptors)
            {
                _descriptors = descriptors ?? new ViewComponentDescriptor[0];
            }

            public IEnumerable<ViewComponentDescriptor> GetViewComponents()
            {
                return _descriptors;
            }
        }

        private static string ReadBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(response.Body))
            {
                return reader.ReadToEnd();
            }
        }

        private class TextViewComponent : ViewComponent
        {
            public HtmlString Invoke(string name)
            {
                return new HtmlString("Hello, " + name);
            }
        }

        private class AsyncTextViewComponent : ViewComponent
        {
            public Task<HtmlString> InvokeAsync(string name)
            {
                return Task.FromResult(new HtmlString("Hello-Async, " + name));
            }
        }
    }
}
