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

public class AcceptedResultTests
{
    public static TheoryData<object> ValuesData
    {
        get
        {
            return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new object(),
                };
        }
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public void Constructor_InitializesStatusCodeAndValue(object value)
    {
        // Arrange & Act
        var result = new AcceptedResult("testlocation", value);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Same(value, result.Value);
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public async Task ExecuteResultAsync_SetsObjectValueOfFormatter(object value)
    {
        // Arrange
        var location = "/test/";
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        object actual = null;
        formatter.Setup(f => f.WriteAsync(It.IsAny<OutputFormatterWriteContext>()))
            .Callback((OutputFormatterWriteContext context) => actual = context.Object)
            .Returns(Task.FromResult(0));

        var actionContext = GetActionContext(httpContext);

        // Act
        var result = new AcceptedResult(location, value);
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Same(value, actual);
    }

    [Fact]
    public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
    {
        // Arrange
        var location = "/test/";
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        var actionContext = GetActionContext(httpContext);

        // Act
        var result = new AcceptedResult(location, "testInput");
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(location, httpContext.Response.Headers["Location"]);
    }

    [Fact]
    public async Task ExecuteResultAsync_OverwritesLocationHeader()
    {
        // Arrange
        var location = "/test/";
        var formatter = CreateMockFormatter();
        var httpContext = GetHttpContext(formatter);
        var actionContext = GetActionContext(httpContext);
        httpContext.Response.Headers["Location"] = "/different/location/";

        // Act
        var result = new AcceptedResult(location, "testInput");
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
        Assert.Equal(location, httpContext.Response.Headers["Location"]);
    }

    private static ActionContext GetActionContext(HttpContext httpContext)
    {
        var routeData = new RouteData();
        routeData.Routers.Add(Mock.Of<IRouter>());
        return new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor());
    }

    private static HttpContext GetHttpContext(Mock<IOutputFormatter> formatter)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices(formatter);
        return httpContext;
    }

    private static Mock<IOutputFormatter> CreateMockFormatter()
    {
        var formatter = new Mock<IOutputFormatter>
        {
            CallBase = true
        };
        formatter.Setup(f => f.CanWriteResult(It.IsAny<OutputFormatterWriteContext>())).Returns(true);

        return formatter;
    }

    private static IServiceProvider CreateServices(Mock<IOutputFormatter> formatter)
    {
        var options = Options.Create(new MvcOptions());
        options.Value.OutputFormatters.Add(formatter.Object);
        var services = new ServiceCollection();
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));

        return services.BuildServiceProvider();
    }
}
