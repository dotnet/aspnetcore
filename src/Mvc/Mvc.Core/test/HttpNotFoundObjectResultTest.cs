// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

public class HttpNotFoundObjectResultTest
{
    [Fact]
    public void HttpNotFoundObjectResult_InitializesStatusCode()
    {
        // Arrange & act
        var notFound = new NotFoundObjectResult(null);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void HttpNotFoundObjectResult_InitializesStatusCodeAndResponseContent()
    {
        // Arrange & act
        var notFound = new NotFoundObjectResult("Test Content");

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
        Assert.Equal("Test Content", notFound.Value);
    }

    [Fact]
    public async Task HttpNotFoundObjectResult_ExecuteSuccessful()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var actionContext = new ActionContext()
        {
            HttpContext = httpContext,
        };

        var result = new NotFoundObjectResult("Test Content");

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
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
