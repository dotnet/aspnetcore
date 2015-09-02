// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.TestCommon.Notification;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Notification;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ActionResults
{
    public class ViewResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_ReturnsError_IfViewCouldNotBeFound()
        {
            // Arrange
            var expected = string.Join(Environment.NewLine,
                                       "The view 'MyView' was not found. The following locations were searched:",
                                       "Location1",
                                       "Location2.");
            
            var actionContext = new ActionContext(GetHttpContext(),
                                                  new RouteData(),
                                                  new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            viewEngine.Setup(v => v.FindView(It.IsAny<ActionContext>(), It.IsAny<string>()))
                      .Returns(ViewEngineResult.NotFound("MyView", new[] { "Location1", "Location2" }))
                       .Verifiable();

            var viewResult = new ViewResult
            {
                ViewEngine = viewEngine.Object,
                ViewName = "MyView"
            };

            // Act and Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                                () => viewResult.ExecuteResultAsync(actionContext));
            Assert.Equal(expected, ex.Message);
            viewEngine.Verify();
        }

        [Fact]
        public async Task ViewResult_UsesFindViewOnSpecifiedViewEngineToLocateViews()
        {
            // Arrange
            var viewName = "myview";
            var context = new ActionContext(GetHttpContext(), new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine.Setup(e => e.FindView(context, "myview"))
                      .Returns(ViewEngineResult.Found("myview", view))
                      .Verifiable();

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }

        public static TheoryData<MediaTypeHeaderValue, string> ViewResultContentTypeData
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
        [MemberData(nameof(ViewResultContentTypeData))]
        public async Task ViewResult_SetsContentTypeHeader(
            MediaTypeHeaderValue contentType,
            string expectedContentTypeHeaderValue)
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();
            var contentTypeBeforeViewResultExecution = contentType?.ToString();

            viewEngine.Setup(e => e.FindView(context, "myview"))
                      .Returns(ViewEngineResult.Found("myview", view));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                ContentType = contentType
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            Assert.Equal(expectedContentTypeHeaderValue, httpContext.Response.ContentType);

            // Check if the original instance provided by the user has not changed.
            // Since we do not have access to the new instance created within the view executor,
            // check if at least the content is the same.
            var contentTypeAfterViewResultExecution = contentType?.ToString();
            Assert.Equal(contentTypeBeforeViewResultExecution, contentTypeAfterViewResultExecution);
        }

        [Fact]
        public async Task ViewResult_SetsStatusCode()
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine.Setup(e => e.FindView(context, "myview"))
                      .Returns(ViewEngineResult.Found("myview", view));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
                StatusCode = 404,
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
            var context = new ActionContext(GetHttpContext(),
                                            new RouteData(),
                                            new ActionDescriptor { Name = viewName });
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(e => e.FindView(context, viewName))
                      .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                      .Verifiable();

            var viewResult = new ViewResult
            {
                ViewEngine = viewEngine.Object
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
            var viewName = "some-view-name";
            var context = new ActionContext(new DefaultHttpContext(),
                                            new RouteData(),
                                            new ActionDescriptor { Name = viewName });
            var viewEngine = new Mock<ICompositeViewEngine>();
            viewEngine.Setup(e => e.FindView(context, viewName))
                      .Returns(ViewEngineResult.Found(viewName, Mock.Of<IView>()))
                      .Verifiable();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(s => s.GetService(typeof(INotifier)))
                .Returns(new Notifier(new ProxyNotifierMethodAdapter()));
            serviceProvider.Setup(p => p.GetService(typeof(ICompositeViewEngine)))
                           .Returns(viewEngine.Object);
            serviceProvider.Setup(p => p.GetService(typeof(ILogger<ViewResult>)))
                           .Returns(new Mock<ILogger<ViewResult>>().Object);
            serviceProvider.Setup(s => s.GetService(typeof(IOptions<MvcViewOptions>)))
                .Returns(() => {
                    var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
                    optionsAccessor.SetupGet(o => o.Value)
                        .Returns(new MvcViewOptions());
                    return optionsAccessor.Object;
                });
            context.HttpContext.RequestServices = serviceProvider.Object;

            var viewResult = new ViewResult
            {
                ViewName = viewName
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            viewEngine.Verify();
        }

        [Fact]
        public async Task ViewResult_NotifiesViewFound()
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var listener = new TestNotificationListener();
            httpContext.RequestServices.GetRequiredService<INotifier>().EnlistTarget(listener);

            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine.Setup(e => e.FindView(context, "myview"))
                      .Returns(ViewEngineResult.Found("myview", view));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
            };

            // Act
            await viewResult.ExecuteResultAsync(context);

            // Assert
            Assert.NotNull(listener.ViewResultViewFound);
            Assert.NotNull(listener.ViewResultViewFound.ActionContext);
            Assert.NotNull(listener.ViewResultViewFound.Result);
            Assert.NotNull(listener.ViewResultViewFound.View);
            Assert.Equal("myview", listener.ViewResultViewFound.ViewName);
        }

        [Fact]
        public async Task ViewResult_NotifiesViewNotFound()
        {
            // Arrange
            var viewName = "myview";
            var httpContext = GetHttpContext();
            var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var listener = new TestNotificationListener();
            httpContext.RequestServices.GetRequiredService<INotifier>().EnlistTarget(listener);

            var viewEngine = new Mock<IViewEngine>();
            var view = Mock.Of<IView>();

            viewEngine.Setup(e => e.FindView(context, "myview"))
                      .Returns(ViewEngineResult.NotFound("myview", new string[] { "location/myview" }));

            var viewResult = new ViewResult
            {
                ViewName = viewName,
                ViewEngine = viewEngine.Object,
            };

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await viewResult.ExecuteResultAsync(context));

            // Assert
            Assert.NotNull(listener.ViewResultViewNotFound);
            Assert.NotNull(listener.ViewResultViewNotFound.ActionContext);
            Assert.NotNull(listener.ViewResultViewNotFound.Result);
            Assert.Equal(new string[] { "location/myview" }, listener.ViewResultViewNotFound.SearchedLocations);
            Assert.Equal("myview", listener.ViewResultViewNotFound.ViewName);
        }

        private HttpContext GetHttpContext()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(s => s.GetService(typeof(ILogger<ViewResult>)))
                .Returns(new Mock<ILogger<ViewResult>>().Object);

            var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
            optionsAccessor.SetupGet(o => o.Value)
                .Returns(new MvcViewOptions());

            serviceProvider.Setup(s => s.GetService(typeof(IOptions<MvcViewOptions>)))
                .Returns(optionsAccessor.Object);

            serviceProvider.Setup(s => s.GetService(typeof(INotifier)))
                .Returns(new Notifier(new ProxyNotifierMethodAdapter()));

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider.Object;

            return httpContext;
        }
    }
}