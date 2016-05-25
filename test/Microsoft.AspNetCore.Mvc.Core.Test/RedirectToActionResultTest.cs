// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc
{
    public class RedirectToActionResultTest
    {
        [Fact]
        public async Task RedirectToAction_Execute_PassesCorrectValuesToRedirect()
        {
            // Arrange
            var expectedUrl = "SampleAction";
            var expectedPermanentFlag = false;

            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(o => o.RequestServices)
                .Returns(CreateServices().BuildServiceProvider());

            var httpResponse = new Mock<HttpResponse>();
            httpContext
                .Setup(o => o.Response)
                .Returns(httpResponse.Object);

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

            var urlHelper = GetMockUrlHelper(expectedUrl);
            var result = new RedirectToActionResult("SampleAction", null, null)
            {
                UrlHelper = urlHelper,
            };

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            // Verifying if Redirect was called with the specific Url and parameter flag.
            // Thus we verify that the Url returned by UrlHelper is passed properly to
            // Redirect method and that the method is called exactly once.
            httpResponse.Verify(r => r.Redirect(expectedUrl, expectedPermanentFlag), Times.Exactly(1));
        }

        [Fact]
        public async Task RedirectToAction_Execute_ThrowsOnNullUrl()
        {
            // Arrange
            var httpContext = new Mock<HttpContext>();
            httpContext
                .Setup(o => o.Response)
                .Returns(new Mock<HttpResponse>().Object);
            httpContext
                .SetupGet(o => o.RequestServices)
                .Returns(CreateServices().BuildServiceProvider());

            var actionContext = new ActionContext(httpContext.Object, new RouteData(), new ActionDescriptor());

            var urlHelper = GetMockUrlHelper(returnValue: null);
            var result = new RedirectToActionResult(null, null, null)
            {
                UrlHelper = urlHelper,
            };

            // Act & Assert
            await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
                async () =>
                {
                    await result.ExecuteResultAsync(actionContext);
                },
                "No route matches the supplied values.");
        }

        private static IUrlHelper GetMockUrlHelper(string returnValue)
        {
            var urlHelper = new Mock<IUrlHelper>();
            urlHelper.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);

            return urlHelper.Object;
        }

        private static IServiceCollection CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<RedirectToActionResultExecutor>();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            return services;
        }
    }
}
