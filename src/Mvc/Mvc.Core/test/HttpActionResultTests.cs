// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class HttpActionResultTests
{
    [Fact]
    public void HttpActionResult_InitializesWithResultsStaticMethods()
    {
        // Arrange & Act
        var httpResult = Mock.Of<IResult>();
        var result = new HttpActionResult(httpResult);

        // Assert
        Assert.Equal(httpResult, result.Result);
    }

    [Fact]
    public async Task HttpActionResult_InvokesInternalHttpResult()
    {
        // Arrange
        var httpContext = new DefaultHttpContext
        {
            RequestServices = CreateServices().BuildServiceProvider()
        };

        var context = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var httpResult = new Mock<IResult>();
        httpResult.Setup(s => s.ExecuteAsync(httpContext))
            .Returns(() => Task.CompletedTask)
            .Verifiable();
        var result = new HttpActionResult(httpResult.Object);

        // Act
        await result.ExecuteResultAsync(context);

        // Assert
        httpResult.Verify();
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
