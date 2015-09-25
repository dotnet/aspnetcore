// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class PartialViewResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_ReturnsError_IfViewCouldNotBeFound()
        {
            // Arrange
            var expected = string.Join(
                Environment.NewLine,
                "The view 'MyView' was not found. The following locations were searched:",
                "Location1",
                "Location2.");
            var actionContext = new ActionContext(
                GetHttpContext(),
                new RouteData(),
                new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            viewEngine
                .Setup(v => v.FindPartialView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = "MyView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task PartialViewResult_UsesFindPartialViewOnSpecifiedViewEngineToLocateViews()
        {
            // Arrange
            var viewName = "myview";
            var context = new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine
                .Setup(e => e.FindPartialView(context, "myview"))
                .Returns(ViewEngineResult.Found("myview", view))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }

        public static TheoryData<MediaTypeHeaderValue, string> PartialViewResultContentTypeData
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
                        new MediaTypeHeaderValue("text/foo") { Encoding = Encoding.ASCII },
                        "text/foo; charset=us-ascii"
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(PartialViewResultContentTypeData))]
        public async Task PartialViewResult_SetsContentTypeHeader(
            MediaTypeHeaderValue contentType,
            string expectedContentTypeHeaderValue)
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine
                .Setup(e => e.FindPartialView(context, "myview"))
                .Returns(ViewEngineResult.Found("myview", view));

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ContentType = contentType,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(expectedContentTypeHeaderValue, httpContext.Response.ContentType);
        }

        [Fact]
        public async Task PartialViewResult_SetsStatusCode()
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine
                .Setup(e => e.FindPartialView(context, "myview"))
                .Returns(ViewEngineResult.Found("myview", view));

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                StatusCode = 404,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(404, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesActionDescriptorName_IfViewNameIsNull()
        {
            // Arrange
            var viewName = "some-view-name";
            var context = new ActionContext(
                GetHttpContext(),
                new RouteData(),
                new ActionDescriptor { Name = viewName });
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(e => e.FindPartialView(context, viewName))
                .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                .Verifiable();

            var viewResult = new PartialViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }

        [Fact]
        public async Task ExecuteResultAsync_UsesCompositeViewEngineFromServices_IfViewEngineIsNotSpecified()
        {
            // Arrange
            var viewName = "partial-view-name";
            var context = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor { Name = viewName });
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine
                .Setup(e => e.FindPartialView(It.IsAny<ActionContext>(), viewName))
                .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                .Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(p => p.GetService(typeof(ICompositeViewEngine)))
                .Returns(viewEngine.Object);
            serviceProvider
                .Setup(p => p.GetService(typeof(ILogger<PartialViewResult>)))
                .Returns(new Mock<ILogger<PartialViewResult>>().Object);
            serviceProvider.Setup(s => s.GetService(typeof(IOptions<MvcViewOptions>)))
                .Returns(() => {
                    var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
                    optionsAccessor.SetupGet(o => o.Value)
                        .Returns(new MvcViewOptions());
                    return optionsAccessor.Object;
                });
            context.HttpContext.RequestServices = serviceProvider.Object;

            var viewResult = new PartialViewResult
            {
                ViewName = viewName,
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider()),
                TempData = Mock.Of<ITempDataDictionary>(),
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }

        private HttpContext GetHttpContext()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<PartialViewResult>)))
                .Returns(new Mock<ILogger<PartialViewResult>>().Object);

            serviceProvider.Setup(s => s.GetService(typeof(IOptions<MvcViewOptions>)))
                .Returns(() => {
                    var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
                    optionsAccessor.SetupGet(o => o.Value)
                        .Returns(new MvcViewOptions());
                    return optionsAccessor.Object;
                });

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider.Object;

            return httpContext;
        }
    }
}