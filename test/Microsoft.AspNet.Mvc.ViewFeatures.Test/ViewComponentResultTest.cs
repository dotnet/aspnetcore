// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentResultTest
    {
        private readonly ITempDataDictionary _tempDataDictionary =
            new TempDataDictionary(new HttpContextAccessor(), new SessionStateTempDataProvider());

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
            var expected = "A view component named 'Text' could not be found.";

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
            var expected = $"A view component named '{typeof(TextViewComponent).FullName}' could not be found.";

            var services = CreateServices();
            services.AddSingleton<IViewComponentSelector>();

            var actionContext = CreateActionContext();

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
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
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
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.AsyncText",
                ShortName = "AsyncText",
                Type = typeof(AsyncTextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
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
        public async Task ExecuteResultAsync_ExecutesViewComponent_ByShortName()
        {
            // Arrange
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
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
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
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
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
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
        public async Task ExecuteResultAsync_SetsStatusCode()
        {
            // Arrange
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
                ViewComponentType = typeof(TextViewComponent),
                StatusCode = 404,
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(404, actionContext.HttpContext.Response.StatusCode);
        }

        public static TheoryData<MediaTypeHeaderValue, string> ViewComponentResultContentTypeData
        {
            get
            {
                return new TheoryData<MediaTypeHeaderValue, string>
                {
                    {
                        null,
                        "text/html; charset=utf-8"
                    },
                    {
                        new MediaTypeHeaderValue("text/foo"),
                        "text/foo; charset=utf-8"
                    },
                    {
                        MediaTypeHeaderValue.Parse("text/foo;p1=p1-value"),
                        "text/foo; p1=p1-value; charset=utf-8"
                    },
                    {
                        new MediaTypeHeaderValue("text/foo") { Encoding = Encoding.ASCII },
                        "text/foo; charset=us-ascii"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewComponentResultContentTypeData))]
        public async Task ViewComponentResult_SetsContentTypeHeader(
            MediaTypeHeaderValue contentType,
            string expectedContentTypeHeaderValue)
        {
            // Arrange
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var contentTypeBeforeViewResultExecution = contentType?.ToString();

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
                ViewComponentName = "Text",
                ContentType = contentType,
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentTypeHeaderValue, actionContext.HttpContext.Response.ContentType);

            // Check if the original instance provided by the user has not changed.
            // Since we do not have access to the new instance created within the view executor,
            // check if at least the content is the same.
            var contentTypeAfterViewResultExecution = contentType?.ToString();
            Assert.Equal(contentTypeBeforeViewResultExecution, contentTypeAfterViewResultExecution);
        }

        [Fact]
        public async Task ViewComponentResult_SetsContentTypeHeader_OverrideResponseContentType()
        {
            // Arrange
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var expectedContentType = "text/html; charset=utf-8";
            actionContext.HttpContext.Response.ContentType = "application/x-will-be-overridden";

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
                ViewComponentName = "Text",
                ContentType = new MediaTypeHeaderValue("text/html") { Encoding = Encoding.UTF8 },
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.ContentType);
        }

        [Fact]
        public async Task ViewComponentResult_NoContentTypeSet_PreservesResponseContentType()
        {
            // Arrange
            var descriptor = new ViewComponentDescriptor()
            {
                FullName = "Full.Name.Text",
                ShortName = "Text",
                Type = typeof(TextViewComponent),
            };

            var actionContext = CreateActionContext(descriptor);

            var expectedContentType = "application/x-will-not-be-overridden";
            actionContext.HttpContext.Response.ContentType = expectedContentType;

            var viewComponentResult = new ViewComponentResult()
            {
                Arguments = new object[] { "World!" },
                ViewComponentName = "Text",
                TempData = _tempDataDictionary,
            };

            // Act
            await viewComponentResult.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(expectedContentType, actionContext.HttpContext.Response.ContentType);
        }

        private IServiceCollection CreateServices(params ViewComponentDescriptor[] descriptors)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<MvcViewOptions>, TestOptionsManager<MvcViewOptions>>();
            services.AddTransient<IViewComponentHelper, DefaultViewComponentHelper>();
            services.AddSingleton<IViewComponentSelector, DefaultViewComponentSelector>();
            services.AddSingleton<IViewComponentDescriptorCollectionProvider, DefaultViewComponentDescriptorCollectionProvider>();
            services.AddSingleton<IViewComponentInvokerFactory, DefaultViewComponentInvokerFactory>();
            services.AddSingleton<ITypeActivatorCache, DefaultTypeActivatorCache>();
            services.AddSingleton<IViewComponentActivator, DefaultViewComponentActivator>();
            services.AddInstance<IViewComponentDescriptorProvider>(new FixedSetViewComponentDescriptorProvider(descriptors));
            services.AddSingleton<IModelMetadataProvider, EmptyModelMetadataProvider>();
            services.AddInstance<ILoggerFactory>(NullLoggerFactory.Instance);

            return services;
        }

        private HttpContext CreateHttpContext(params ViewComponentDescriptor[] descriptors)
        {
            var services = CreateServices(descriptors);

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.RequestServices = services.BuildServiceProvider();

            return httpContext;
        }

        private ActionContext CreateActionContext(params ViewComponentDescriptor[] descriptors)
        {
            return new ActionContext(CreateHttpContext(descriptors), new RouteData(), new ActionDescriptor());
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

        private class TextViewComponent : ViewComponent
        {
            public HtmlString Invoke(string name)
            {
                return new HtmlString("Hello, " + name);
            }
        }

        private class AsyncTextViewComponent : ViewComponent
        {
            public HtmlString Invoke()
            {
                // Should never run.
                throw null;
            }

            public Task<HtmlString> InvokeAsync(string name)
            {
                return Task.FromResult(new HtmlString("Hello-Async, " + name));
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
    }
}