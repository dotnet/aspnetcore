// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class CreatedResultTests
{
    [Fact]
    public void CreatedResult_SetsStatusCode()
    {
        // Act
        var result = new CreatedResult();

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public void CreatedResult_SetsLocation()
    {
        // Arrange
        var location = "http://test/location";

        // Act
        var result = new CreatedResult(location, "testInput");

        // Assert
        Assert.Same(location, result.Location);
    }

    [Fact]
    public void CreatedResult_WithNoArgs_SetsLocationNull()
    {
        // Act
        var result = new CreatedResult();

        // Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void CreatedResult_SetsLocationNull()
    {
        // Act
        var result = new CreatedResult((string)null, "testInput");

        // Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task CreatedResult_ReturnsStatusCode_SetsLocationHeader()
    {
        // Arrange
        var location = "/test/";
        var httpContext = GetHttpContext();
        var actionContext = GetActionContext(httpContext);
        var result = new CreatedResult(location, "testInput");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(location, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task CreatedResult_ReturnsStatusCode_NotSetLocationHeader()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = GetActionContext(httpContext);
        var result = new CreatedResult((string)null, "testInput");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Headers["Location"].Count);
    }

    [Fact]
    public async Task CreatedResult_OverwritesLocationHeader()
    {
        // Arrange
        var location = "/test/";
        var httpContext = GetHttpContext();
        var actionContext = GetActionContext(httpContext);
        httpContext.Response.Headers["Location"] = "/different/location/";
        var result = new CreatedResult(location, "testInput");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, httpContext.Response.StatusCode);
        Assert.Equal(location, httpContext.Response.Headers["Location"]);
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
}
