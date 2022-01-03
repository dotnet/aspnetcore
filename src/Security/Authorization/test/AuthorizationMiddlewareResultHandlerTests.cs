// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Authorization.Test;

public class AuthorizationMiddlewareResultHandlerTests
{
    [Fact]
    public async Task CallRequestDelegate_If_PolicyAuthorizationResultSucceeded()
    {
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Success();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        requestDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task NotCallRequestDelegate_If_PolicyAuthorizationResultWasChallenged()
    {
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Challenge();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        requestDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task NotCallRequestDelegate_If_PolicyAuthorizationResultWasForbidden()
    {
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Forbid();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        requestDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task ChallangeEachAuthenticationScheme_If_PolicyAuthorizationResultWasChallenged()
    {
        var authenticationServiceMock = new Mock<IAuthenticationService>();
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext(authenticationServiceMock.Object);
        var firstScheme = Guid.NewGuid().ToString();
        var secondScheme = Guid.NewGuid().ToString();
        var thirdScheme = Guid.NewGuid().ToString();
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(firstScheme, secondScheme, thirdScheme)
            .Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Challenge();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        authenticationServiceMock.Verify(service => service.ChallengeAsync(httpContext, It.IsAny<string>(), null), Times.Exactly(3));
        authenticationServiceMock.Verify(service => service.ChallengeAsync(httpContext, firstScheme, null), Times.Once);
        authenticationServiceMock.Verify(service => service.ChallengeAsync(httpContext, secondScheme, null), Times.Once);
        authenticationServiceMock.Verify(service => service.ChallengeAsync(httpContext, thirdScheme, null), Times.Once);
    }

    [Fact]
    public async Task ChallangeWithoutAuthenticationScheme_If_PolicyAuthorizationResultWasChallenged()
    {
        var authenticationServiceMock = new Mock<IAuthenticationService>();
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext(authenticationServiceMock.Object);
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Challenge();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        authenticationServiceMock.Verify(service => service.ChallengeAsync(httpContext, null, null), Times.Once);
    }

    [Fact]
    public async Task ForbidEachAuthenticationScheme_If_PolicyAuthorizationResultWasForbidden()
    {
        var authenticationServiceMock = new Mock<IAuthenticationService>();
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext(authenticationServiceMock.Object);
        var firstScheme = Guid.NewGuid().ToString();
        var secondScheme = Guid.NewGuid().ToString();
        var thirdScheme = Guid.NewGuid().ToString();
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(firstScheme, secondScheme, thirdScheme)
            .Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Forbid();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        authenticationServiceMock.Verify(service => service.ForbidAsync(httpContext, It.IsAny<string>(), null), Times.Exactly(3));
        authenticationServiceMock.Verify(service => service.ForbidAsync(httpContext, firstScheme, null), Times.Once);
        authenticationServiceMock.Verify(service => service.ForbidAsync(httpContext, secondScheme, null), Times.Once);
        authenticationServiceMock.Verify(service => service.ForbidAsync(httpContext, thirdScheme, null), Times.Once);
    }

    [Fact]
    public async Task ForbidWithoutAuthenticationScheme_If_PolicyAuthorizationResultWasForbidden()
    {
        var authenticationServiceMock = new Mock<IAuthenticationService>();
        var requestDelegate = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContext(authenticationServiceMock.Object);
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var policyAuthorizationResult = PolicyAuthorizationResult.Forbid();
        var handler = CreateAuthorizationMiddlewareResultHandler();

        await handler.HandleAsync(requestDelegate.Object, httpContext, policy, policyAuthorizationResult);

        authenticationServiceMock.Verify(service => service.ForbidAsync(httpContext, null, null), Times.Once);
    }

    private HttpContext CreateHttpContext(IAuthenticationService authenticationService = null)
    {
        var services = new ServiceCollection();

        services.AddTransient(provider => authenticationService ?? new Mock<IAuthenticationService>().Object);

        var serviceProvider = services.BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = serviceProvider };
    }

    private AuthorizationMiddlewareResultHandler CreateAuthorizationMiddlewareResultHandler() => new AuthorizationMiddlewareResultHandler();
}
