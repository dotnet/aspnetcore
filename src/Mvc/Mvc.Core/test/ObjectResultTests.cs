// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

public class ObjectResultTests
{
    [Fact]
    public void ObjectResult_Constructor()
    {
        // Arrange
        var input = "testInput";

        // Act
        var result = new ObjectResult(input);

        // Assert
        Assert.Equal(input, result.Value);
        Assert.Empty(result.ContentTypes);
        Assert.Empty(result.Formatters);
        Assert.Null(result.StatusCode);
        Assert.Null(result.DeclaredType);
    }

    [Fact]
    public async Task ObjectResult_ExecuteResultAsync_SetsStatusCode()
    {
        // Arrange
        var result = new ObjectResult("Hello")
        {
            StatusCode = 404,
            Formatters = new FormatterCollection<IOutputFormatter>()
                {
                    new NoOpOutputFormatter(),
                },
        };

        var actionContext = new ActionContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            }
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(404, actionContext.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ObjectResult_ExecuteResultAsync_SetsProblemDetailsStatus()
    {
        // Arrange
        var modelState = new ModelStateDictionary();

        var details = new ValidationProblemDetails(modelState);

        var result = new ObjectResult(details)
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            Formatters = new FormatterCollection<IOutputFormatter>()
                {
                    new NoOpOutputFormatter(),
                },
        };

        var actionContext = new ActionContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            }
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
    }

    [Fact]
    public async Task ObjectResult_ExecuteResultAsync_GetsStatusCodeFromProblemDetails()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status413RequestEntityTooLarge, };

        var result = new ObjectResult(details)
        {
            Formatters = new FormatterCollection<IOutputFormatter>()
                {
                    new NoOpOutputFormatter(),
                },
        };

        var actionContext = new ActionContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            }
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, details.Status.Value);
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, result.StatusCode.Value);
        Assert.Equal(StatusCodes.Status413RequestEntityTooLarge, actionContext.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ObjectResult_ExecuteResultAsync_ResultAndProblemDetailsHaveStatusCodes()
    {
        // Arrange
        var details = new ProblemDetails { Status = StatusCodes.Status422UnprocessableEntity, };

        var result = new BadRequestObjectResult(details)
        {
            Formatters = new FormatterCollection<IOutputFormatter>()
                {
                    new NoOpOutputFormatter(),
                },
        };

        var actionContext = new ActionContext()
        {
            HttpContext = new DefaultHttpContext()
            {
                RequestServices = CreateServices(),
            }
        };

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, details.Status.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, actionContext.HttpContext.Response.StatusCode);
    }

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        var options = Options.Create(new MvcOptions());
        services.AddSingleton<IActionResultExecutor<ObjectResult>>(new ObjectResultExecutor(
            new DefaultOutputFormatterSelector(options, NullLoggerFactory.Instance),
            new TestHttpResponseStreamWriterFactory(),
            NullLoggerFactory.Instance,
            options));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        return services.BuildServiceProvider();
    }

    private class NoOpOutputFormatter : IOutputFormatter
    {
        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return true;
        }

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            return Task.FromResult(0);
        }
    }
}
