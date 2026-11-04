// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ChallengeResultTest
{
    [Fact]
    public async Task ChallengeResult_ExecuteResultAsync()
    {
        // Arrange
        var result = new ChallengeResult("", null);

        var auth = new Mock<IAuthenticationService>();

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.RequestServices)
            .Returns(CreateServices().AddSingleton(auth.Object).BuildServiceProvider());

        var routeData = new RouteData();
        routeData.Routers.Add(Mock.Of<IRouter>());

        var actionContext = new ActionContext(httpContext.Object,
                                              routeData,
                                              new ActionDescriptor());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        auth.Verify(c => c.ChallengeAsync(httpContext.Object, "", null), Times.Exactly(1));
    }

    [Fact]
    public async Task ChallengeResult_ExecuteResultAsync_NoSchemes()
    {
        // Arrange
        var result = new ChallengeResult(new string[] { }, null);

        var auth = new Mock<IAuthenticationService>();
        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(c => c.RequestServices)
            .Returns(CreateServices().AddSingleton(auth.Object).BuildServiceProvider());

        var routeData = new RouteData();
        routeData.Routers.Add(Mock.Of<IRouter>());

        var actionContext = new ActionContext(httpContext.Object,
                                              routeData,
                                              new ActionDescriptor());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        auth.Verify(c => c.ChallengeAsync(httpContext.Object, null, null), Times.Exactly(1));
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAuthenticationCore();
        return services;
    }
}
