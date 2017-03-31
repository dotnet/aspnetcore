// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages
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

            var urlHelper = GetUrlHelper(returnValue: null);
            var result = new RedirectToPageResult("some-page", new Dictionary<string, object>())
            {
                UrlHelper = urlHelper,
            };

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                () => result.ExecuteResultAsync(actionContext),
                "No page named 'some-page' matches the supplied values.");
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

            var urlHelper = GetUrlHelper(expectedUrl);
            var result = new RedirectToPageResult("MyPage", new { id = 10, test = "value" }, permanentRedirect)
            {
                UrlHelper = urlHelper,
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            httpResponse.Verify(r => r.Redirect(expectedUrl, permanentRedirect), Times.Exactly(1));
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
            };

            UrlRouteContext context = null;
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Callback((UrlRouteContext c) => context = c)
                .Returns("some-value");
            var values = new { test = "test-value" };
            var result = new RedirectToPageResult("MyPage", values, true, "test-fragment")
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
                    Assert.Equal("MyPage", value.Value);
                });
            Assert.Equal("ftp", context.Protocol);
            Assert.Equal("test-fragment", context.Fragment);
        }

        private static IServiceProvider CreateServices(IUrlHelperFactory factory = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<RedirectToPageResultExecutor>();

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

        private static IUrlHelper GetUrlHelper(string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(o => o.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(returnValue);
            return urlHelper.Object;
        }
    }
}
