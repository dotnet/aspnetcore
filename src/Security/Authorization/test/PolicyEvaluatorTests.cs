// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authorization.Policy.Test;

public class PolicyEvaluatorTests
{
    [Fact]
    public async Task AuthenticateFailsIfNoPrincipalReturned()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var services = new ServiceCollection().AddSingleton<IAuthenticationService, SadAuthentication>();
        context.RequestServices = services.BuildServiceProvider();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        // Act
        var result = await evaluator.AuthenticateAsync(policy, context);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticateMergeSchemes()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var services = new ServiceCollection().AddSingleton<IAuthenticationService, EchoAuthentication>();
        context.RequestServices = services.BuildServiceProvider();
        var policy = new AuthorizationPolicyBuilder().AddAuthenticationSchemes("A", "B", "C").RequireAssertion(_ => true).Build();

        // Act
        var result = await evaluator.AuthenticateAsync(policy, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Principal.Identities.Count());
    }

    [Fact]
    public async Task AuthenticateMergeSchemesPreservesSingleScheme()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var auth = new EchoAuthentication();
        var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth);
        context.RequestServices = services.BuildServiceProvider();
        var policy = new AuthorizationPolicyBuilder().AddAuthenticationSchemes("A").RequireAssertion(_ => true).Build();

        // Act
        var result = await evaluator.AuthenticateAsync(policy, context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(result.Principal.Identities);
        Assert.Equal(auth.Principal, result.Principal);
    }

    [Fact]
    public async Task AuthorizeSucceedsEvenIfAuthenticationFails()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();

        // Act
        var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Fail("Nooo"), context, resource: null);

        // Assert
        Assert.True(result.Succeeded);
        Assert.False(result.Challenged);
        Assert.False(result.Forbidden);
    }

    [Fact]
    public async Task AuthorizeSucceedsOnlyIfResourceSpecified()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(c => c.Resource != null).Build();
        var success = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "whatever"));

        // Act
        var result = await evaluator.AuthorizeAsync(policy, success, context, resource: null);
        var result2 = await evaluator.AuthorizeAsync(policy, success, context, resource: new object());

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result2.Succeeded);
    }

    [Fact]
    public async Task AuthorizeChallengesIfAuthenticationFails()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();

        // Act
        var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Fail("Nooo"), context, resource: null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Challenged);
        Assert.False(result.Forbidden);
    }

    [Fact]
    public async Task AuthorizeForbidsIfAuthenticationSuceeds()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();

        // Act
        var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "scheme")), context, resource: null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.Challenged);
        Assert.True(result.Forbidden);
    }

    [Fact]
    public async Task AuthorizeForbidsAndFailureIsIncludedIfAuthenticationSuceeds()
    {
        // Arrange
        var evaluator = BuildEvaluator();
        var context = new DefaultHttpContext();
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new DummyRequirement())
            .RequireAssertion(_ => false)
            .Build();

        // Act
        var result = await evaluator.AuthorizeAsync(policy, AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), "scheme")), context, resource: null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.Challenged);
        Assert.True(result.Forbidden);
        Assert.NotNull(result.AuthorizationFailure);
        Assert.Contains(result.AuthorizationFailure.FailedRequirements, requirement => requirement is DummyRequirement);
    }

    private IPolicyEvaluator BuildEvaluator(Action<IServiceCollection> setupServices = null)
    {
        var services = new ServiceCollection()
            .AddAuthorization()
            .AddLogging()
            .AddOptions();
        setupServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IPolicyEvaluator>();
    }

    public class HappyAuthorization : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    public class SadAuthorization : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
            => Task.FromResult(AuthorizationResult.Failed());
    }

    public class SadAuthentication : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
            => Task.FromResult(AuthenticateResult.Fail("Sad."));

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            => throw new NotImplementedException();

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            => throw new NotImplementedException();

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
                => throw new NotImplementedException();

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
                => throw new NotImplementedException();
    }

    public class EchoAuthentication : IAuthenticationService
    {
        public ClaimsPrincipal Principal;

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(scheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(Principal, scheme)));
        }

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            => throw new NotImplementedException();

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            => throw new NotImplementedException();

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
            => throw new NotImplementedException();

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
            => throw new NotImplementedException();
    }

    private class DummyRequirement : IAuthorizationRequirement { }
}
