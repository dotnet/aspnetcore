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

namespace Microsoft.AspNetCore.Mvc;

public class HttpOkObjectResultTest
{
    public static TheoryData<object> ValuesData
    {
        get
        {
            return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new Person
                    {
                        Id = 274,
                        Name = "George",
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public void HttpOkObjectResult_InitializesStatusCodeAndValue(object value)
    {
        // Arrange & Act
        var result = new OkObjectResult(value);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Same(value, result.Value);
    }

    [Theory]
    [MemberData(nameof(ValuesData))]
    public async Task HttpOkObjectResult_SetsStatusCode(object value)
    {
        // Arrange
        var result = new OkObjectResult(value);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = CreateServices(),
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
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

    private class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
