// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectToPageResultTest
    {
        [Fact]
        public async Task ExecuteResultAsync_ThrowsOnNullUrl()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = CreateServices(),
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            var urlHelper = GetUrlHelper(actionContext, returnValue: null);
            var result = new RedirectToPageResult("/some-page", new Dictionary<string, object>())
            {
                UrlHelper = urlHelper,
            };

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                () => result.ExecuteResultAsync(actionContext),
                "No page named '/some-page' matches the supplied values.");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExecuteResultAsync_PassesCorrectValuesToRedirect(bool permanentRedirect)
        {
            // Arrange
            var expectedUrl = "SampleAction";

            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.SetupGet(c => c.RequestServices)
                .Returns(CreateServices());
            httpContext.SetupGet(c => c.Response)
                .Returns(httpResponse.Object);

            var actionContext = new ActionContext(
                httpContext.Object,
                new RouteData(),
                new ActionDescriptor());

            var urlHelper = GetUrlHelper(actionContext, expectedUrl);
            var result = new RedirectToPageResult("/MyPage", null, new { id = 10, test = "value" }, permanentRedirect)
            {
                UrlHelper = urlHelper,
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            httpResponse.Verify(r => r.Redirect(expectedUrl, permanentRedirect), Times.Exactly(1));
        }

        [Fact]
        public async Task ExecuteResultAsync_LocalRelativePaths()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = CreateServices(),
            };

            var pageContext = new PageContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new CompiledPageActionDescriptor(),
            };

            pageContext.RouteData.Values.Add("page", "/A/Redirecting/Page");

            UrlRouteContext context = null;
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext).Returns(pageContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext c) => context = c)
                .Returns("some-value");
            var values = new { test = "test-value" };
            var result = new RedirectToPageResult("./", "page-handler", values, true, "test-fragment")
            {
                UrlHelper = urlHelper.Object,
                Protocol = "ftp",
            };

            // Act
            await result.ExecuteResultAsync(pageContext);

            // Assert
            Assert.NotNull(context);
            Assert.Null(context.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(context.Values),
                value =>
                {
                    Assert.Equal("test", value.Key);
                    Assert.Equal("test-value", value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/A/Redirecting", value.Value);
                },
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Equal("page-handler", value.Value);
                });
            Assert.Equal("ftp", context.Protocol);
            Assert.Equal("test-fragment", context.Fragment);
        }

        [Fact]
        public async Task ExecuteResultAsync_WithAllParameters()
        {
            // Arrange
            var httpContext = new DefaultHttpContext
            {
                RequestServices = CreateServices(),
            };

            var pageContext = new PageContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
            };

            UrlRouteContext context = null;
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext).Returns(pageContext);
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext c) => context = c)
                .Returns("some-value");
            var values = new { test = "test-value" };
            var result = new RedirectToPageResult("/MyPage", "page-handler", values, true, "test-fragment")
            {
                UrlHelper = urlHelper.Object,
                Protocol = "ftp",
            };

            // Act
            await result.ExecuteResultAsync(pageContext);

            // Assert
            Assert.NotNull(context);
            Assert.Null(context.RouteName);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(context.Values),
                value =>
                {
                    Assert.Equal("test", value.Key);
                    Assert.Equal("test-value", value.Value);
                },
                value =>
                {
                    Assert.Equal("page", value.Key);
                    Assert.Equal("/MyPage", value.Value);
                },
                value =>
                {
                    Assert.Equal("handler", value.Key);
                    Assert.Equal("page-handler", value.Value);
                });
            Assert.Equal("ftp", context.Protocol);
            Assert.Equal("test-fragment", context.Fragment);
        }

        [Fact]
        public async Task RedirectToPage_WithNullPage_UsesAmbientValue()
        {
            // Arrange
            var expected = "path/to/this-page";
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.SetupGet(c => c.Response)
                .Returns(httpResponse.Object);
            httpContext.SetupGet(c => c.RequestServices)
                .Returns(CreateServices());
            var routeData = new RouteData
            {
                Values =
                {
                    ["page"] = expected,
                }
            };

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            UrlRouteContext context = null;
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext c) => context = c)
                .Returns("some-value");
            urlHelper.SetupGet(h => h.ActionContext)
                .Returns(actionContext);
            var pageName = (string)null;
            var result = new RedirectToPageResult(pageName)
            {
                UrlHelper = urlHelper.Object,
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.NotNull(context);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(context.Values),
               value =>
               {
                   Assert.Equal("page", value.Key);
                   Assert.Equal(expected, value.Value);
               });
        }

        [Fact]
        public async Task RedirectToPage_DoesNotUseAmbientHandler()
        {
            // Arrange
            var expected = "path/to/this-page";
            var httpContext = new Mock<HttpContext>();
            var httpResponse = new Mock<HttpResponse>();
            httpContext.SetupGet(c => c.Response)
                .Returns(httpResponse.Object);
            httpContext.SetupGet(c => c.RequestServices)
                .Returns(CreateServices());
            var routeData = new RouteData
            {
                Values =
                {
                    ["page"] = expected,
                    ["handler"] = "delete",
                }
            };

            var actionContext = new ActionContext(
                httpContext.Object,
                routeData,
                new ActionDescriptor());

            UrlRouteContext context = null;
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext c) => context = c)
                .Returns("some-value");
            urlHelper.SetupGet(h => h.ActionContext)
                .Returns(actionContext);
            var pageName = (string)null;
            var result = new RedirectToPageResult(pageName)
            {
                UrlHelper = urlHelper.Object,
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.NotNull(context);
            Assert.Collection(Assert.IsType<RouteValueDictionary>(context.Values),
               value =>
               {
                   Assert.Equal("page", value.Key);
                   Assert.Equal(expected, value.Value);
               },
               value =>
               {
                   Assert.Equal("handler", value.Key);
                   Assert.Null(value.Value);
               });
        }

        private static IServiceProvider CreateServices(IUrlHelperFactory factory = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<RedirectToPageResult>, RedirectToPageResultExecutor>();

            if (factory != null)
            {
                services.AddSingleton(factory);
            }
            else
            {
                services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            }

            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services.BuildServiceProvider();
        }

        private static IUrlHelper GetUrlHelper(ActionContext context, string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext).Returns(context);
            urlHelper.Setup(o => o.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(returnValue);
            return urlHelper.Object;
        }
    }
}
