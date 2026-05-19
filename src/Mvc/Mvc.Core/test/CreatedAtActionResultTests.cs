// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class CreatedAtActionResultTests
{
    [Fact]
    public async Task CreatedAtActionResult_ReturnsStatusCode_SetsLocationHeader()
    {
        // Arrange
        var expectedUrl = "testAction";
        var httpContext = GetHttpContext();
        var actionContext = GetActionContext(httpContext);
        var urlHelper = GetMockUrlHelper(expectedUrl);

        // Act
        var result = new CreatedAtActionResult(
            actionName: expectedUrl,
            controllerName: null,
            routeValues: null,
            value: null);

        result.UrlHelper = urlHelper;
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(expectedUrl, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task CreatedAtActionResult_ThrowsOnNullUrl()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = GetActionContext(httpContext);
        var urlHelper = GetMockUrlHelper(returnValue: null);

        var result = new CreatedAtActionResult(
            actionName: null,
            controllerName: null,
            routeValues: null,
            value: null);

        result.UrlHelper = urlHelper;

        // Act & Assert
        await ExceptionAssert.ThrowsAsync<InvalidOperationException>(
            async () => await result.ExecuteResultAsync(actionContext),
        "No route matches the supplied values.");
    }

    private static ActionContext GetActionContext(HttpContext httpContext)
    {
        var routeData = new RouteData();
        routeData.Routers.Add(Mock.Of<IRouter>());

        return new ActionContext(httpContext,
                                routeData,
                                new ActionDescriptor());
    }
    private static HttpContext GetHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = new PathString("");
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = CreateServices();
        return httpContext;
    }

    private static IServiceProvider CreateServices()
    {
        var options = Options.Create(new MvcOptions());
        options.Value.OutputFormatters.Add(new StringOutputFormatter());
        options.Value.OutputFormatters.Add(SystemTextJsonOutputFormatter.CreateFormatter(new JsonOptions()));

        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));

        return services.BuildServiceProvider();
    }

    private static IUrlHelper GetMockUrlHelper(string returnValue)
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(o => o.Action(It.IsAny<UrlActionContext>())).Returns(returnValue);

        return urlHelper.Object;
    }
}
