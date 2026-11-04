// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class RedirectToRouteResultTest
{
    [Theory]
    [MemberData(nameof(RedirectToRouteData))]
    public async Task RedirectToRoute_Execute_PassesCorrectValuesToRedirect(object values)
    {
        // Arrange
        var expectedUrl = "SampleAction";
        var expectedPermanentFlag = false;

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(o => o.RequestServices).Returns(CreateServices().BuildServiceProvider());

        var httpResponse = new Mock<HttpResponse>();
        httpContext.Setup(o => o.Response).Returns(httpResponse.Object);

        var actionContext = new ActionContext(httpContext.Object,
                                              new RouteData(),
                                              new ActionDescriptor());

        var urlHelper = GetMockUrlHelper(expectedUrl);
        var result = new RedirectToRouteResult(null, PropertyHelper.ObjectToDictionary(values))
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
    public async Task RedirectToRoute_Execute_ThrowsOnNullUrl()
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
        var result = new RedirectToRouteResult(null, new Dictionary<string, object>())
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

    [Fact]
    public async Task ExecuteResultAsync_UsesRouteName_ToGenerateLocationHeader()
    {
        // Arrange
        var routeName = "orders_api";
        var locationUrl = "/api/orders/10";

        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(uh => uh.RouteUrl(It.IsAny<UrlRouteContext>()))
            .Returns(locationUrl)
            .Verifiable();
        var factory = new Mock<IUrlHelperFactory>();
        factory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper.Object);

        var httpContext = GetHttpContext(factory.Object);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var result = new RedirectToRouteResult(routeName, new { id = 10 });

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        urlHelper.Verify(uh => uh.RouteUrl(
            It.Is<UrlRouteContext>(routeContext => string.Equals(routeName, routeContext.RouteName))));
        Assert.True(httpContext.Response.Headers.ContainsKey("Location"), "Location header not found");
        Assert.Equal(locationUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_WithFragment_PassesCorrectValuesToRedirect()
    {
        // Arrange
        var expectedUrl = "/SampleAction#test";
        var expectedStatusCode = StatusCodes.Status301MovedPermanently;

        var httpContext = GetHttpContext();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var urlHelper = GetMockUrlHelper(expectedUrl);
        var result = new RedirectToRouteResult("Sample", null, true, "test")
        {
            UrlHelper = urlHelper,
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_WithFragment_PassesCorrectValuesToRedirect_WithPreserveMethod()
    {
        // Arrange
        var expectedUrl = "/SampleAction#test";
        var expectedStatusCode = StatusCodes.Status308PermanentRedirect;

        var httpContext = GetHttpContext();

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var urlHelper = GetMockUrlHelper(expectedUrl);
        var result = new RedirectToRouteResult("Sample", null, true, true, "test")
        {
            UrlHelper = urlHelper,
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    private static HttpContext GetHttpContext(IUrlHelperFactory factory = null)
    {
        var services = CreateServices(factory);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }

    private static IServiceCollection CreateServices(IUrlHelperFactory factory = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<RedirectToRouteResult>, RedirectToRouteResultExecutor>();

        if (factory != null)
        {
            services.AddSingleton(factory);
        }
        else
        {
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
        }

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    public static IEnumerable<object[]> RedirectToRouteData
    {
        get
        {
            yield return new object[] { null };
            yield return
                new object[] {
                        new Dictionary<string, string>() { { "hello", "world" } }
                };
            yield return
                new object[] {
                        new RouteValueDictionary(new Dictionary<string, string>() {
                                                        { "test", "case" }, { "sample", "route" } })
                };
        }
    }

    private static IUrlHelper GetMockUrlHelper(string returnValue)
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(o => o.RouteUrl(It.IsAny<UrlRouteContext>())).Returns(returnValue);
        return urlHelper.Object;
    }
}
