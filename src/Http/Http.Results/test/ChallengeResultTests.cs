// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ChallengeResultTests
{
    [Fact]
    public async Task ChallengeResult_ExecuteAsync()
    {
        // Arrange
        var result = new ChallengeHttpResult("", null);
        var auth = new Mock<IAuthenticationService>();
        var httpContext = GetHttpContext(auth);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify(c => c.ChallengeAsync(httpContext, "", null), Times.Exactly(1));
    }

    [Fact]
    public async Task ChallengeResult_ExecuteAsync_NoSchemes()
    {
        // Arrange
        var result = new ChallengeHttpResult(new string[] { }, null);
        var auth = new Mock<IAuthenticationService>();
        var httpContext = GetHttpContext(auth);

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        auth.Verify(c => c.ChallengeAsync(httpContext, null, null), Times.Exactly(1));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsArgumentNullException_WhenHttpContextIsNull()
    {
        // Arrange
        var result = new ChallengeHttpResult();
        HttpContext httpContext = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("httpContext", () => result.ExecuteAsync(httpContext));
    }

    private static DefaultHttpContext GetHttpContext(Mock<IAuthenticationService> auth)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices()
            .AddSingleton(auth.Object)
            .BuildServiceProvider();
        return httpContext;
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }
}
