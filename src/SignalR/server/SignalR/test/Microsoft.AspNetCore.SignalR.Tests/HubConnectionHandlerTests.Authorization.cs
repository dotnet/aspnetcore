// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HubMethodAuthorizationUsesScopedServiceAndCurrentResourceOnEveryInvocation(bool allowsCaching)
    {
        using var log = StartVerifiableLog();
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var policyProvider = new Mock<IAuthorizationPolicyProvider>();
        policyProvider.Setup(p => p.AllowsCachingPolicies).Returns(allowsCaching);
        policyProvider.Setup(p => p.GetPolicyAsync("test")).ReturnsAsync(policy);
        var authorizationServices = new List<Mock<IAuthorizationService>>();
        var resources = new List<HubInvocationContext>();
        var principals = new List<ClaimsPrincipal>();
        var authorized = true;
        using var serviceProvider = (ServiceProvider)HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
        {
            services.AddAuthorization();
            services.AddSingleton(policyProvider.Object);
            services.AddScoped<IAuthorizationService>(_ =>
            {
                var authorizationService = new Mock<IAuthorizationService>();
                authorizationService.Setup(s => s.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                    .Returns((ClaimsPrincipal principal, object resource, IEnumerable<IAuthorizationRequirement> requirements) =>
                    {
                        principals.Add(principal);
                        resources.Add(Assert.IsType<HubInvocationContext>(resource));
                        Assert.Equal(policy.Requirements, requirements);
                        return Task.FromResult(authorized ? AuthorizationResult.Success() : AuthorizationResult.Failed());
                    });
                authorizationServices.Add(authorizationService);
                return authorizationService.Object;
            });
        }, LoggerFactory);
        var handler = serviceProvider.GetRequiredService<HubConnectionHandler<MethodHub>>();
        using var client = new TestClient();
        var connectionTask = await client.ConnectAsync(handler).DefaultTimeout();

        var first = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "first", "value").DefaultTimeout();
        Assert.Null(first.Error);

        authorized = false;
        var second = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "first", "value").DefaultTimeout();
        Assert.NotNull(second.Error);

        authorized = true;
        var third = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "second", "value").DefaultTimeout();
        Assert.Null(third.Error);

        Assert.Equal(3, authorizationServices.Count);
        Assert.All(authorizationServices, service => service.Verify(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()), Times.Once));
        Assert.All(principals, principal => Assert.Same(client.Connection.User, principal));
        Assert.Equal(3, resources.Count);
        Assert.Equal(new object[] { "first", "value" }, resources[0].HubMethodArguments);
        Assert.Equal(new object[] { "first", "value" }, resources[1].HubMethodArguments);
        Assert.Equal(new object[] { "second", "value" }, resources[2].HubMethodArguments);
        Assert.NotSame(resources[0], resources[1]);
        Assert.NotSame(resources[1], resources[2]);
        Assert.NotSame(resources[0].ServiceProvider, resources[1].ServiceProvider);
        Assert.NotSame(resources[1].ServiceProvider, resources[2].ServiceProvider);
        Assert.NotSame(resources[0].Hub, resources[1].Hub);
        policyProvider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(allowsCaching ? 1 : 3));

        client.Dispose();
        await connectionTask.DefaultTimeout();
    }

    [Theory]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public async Task HubMethodAuthorizationDoesNotSharePoliciesAcrossProviderLifetimes(ServiceLifetime lifetime)
    {
        using var log = StartVerifiableLog();
        var providers = new List<Mock<IAuthorizationPolicyProvider>>();
        var policies = new List<AuthorizationPolicy>();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.Setup(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .Returns((ClaimsPrincipal principal, object resource, IEnumerable<IAuthorizationRequirement> requirements) =>
            {
                Assert.Equal(policies[^1].Requirements, requirements);
                return Task.FromResult(providers.Count == 2 ? AuthorizationResult.Failed() : AuthorizationResult.Success());
            });
        using var serviceProvider = (ServiceProvider)HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
        {
            services.AddSingleton(authorizationService.Object);
            services.Add(new ServiceDescriptor(typeof(IAuthorizationPolicyProvider), _ =>
            {
                var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
                policies.Add(policy);
                var provider = new Mock<IAuthorizationPolicyProvider>();
                provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
                provider.Setup(p => p.GetPolicyAsync("test")).ReturnsAsync(policy);
                providers.Add(provider);
                return provider.Object;
            }, lifetime));
        }, LoggerFactory);
        var handler = serviceProvider.GetRequiredService<HubConnectionHandler<MethodHub>>();
        using var client = new TestClient();
        var connectionTask = await client.ConnectAsync(handler).DefaultTimeout();

        Assert.Null((await client.InvokeAsync(nameof(MethodHub.AuthMethod)).DefaultTimeout()).Error);
        Assert.NotNull((await client.InvokeAsync(nameof(MethodHub.AuthMethod)).DefaultTimeout()).Error);
        Assert.Null((await client.InvokeAsync(nameof(MethodHub.AuthMethod)).DefaultTimeout()).Error);
        Assert.Equal(3, providers.Count);
        Assert.All(providers, provider => provider.Verify(p => p.GetPolicyAsync("test"), Times.Once));

        client.Dispose();
        await connectionTask.DefaultTimeout();
    }

    [Fact]
    public async Task HubMethodWithoutPoliciesDoesNotResolveAuthorizationServices()
    {
        using var log = StartVerifiableLog();
        using var serviceProvider = (ServiceProvider)HubConnectionHandlerTestUtils.CreateServiceProvider(services =>
        {
            services.AddScoped<IAuthorizationPolicyProvider>(_ => throw new InvalidOperationException("Should not resolve policy provider."));
            services.AddScoped<IAuthorizationService>(_ => throw new InvalidOperationException("Should not resolve authorization service."));
        }, LoggerFactory);
        var handler = serviceProvider.GetRequiredService<HubConnectionHandler<MethodHub>>();
        using var client = new TestClient();
        var connectionTask = await client.ConnectAsync(handler).DefaultTimeout();

        var result = await client.InvokeAsync(nameof(MethodHub.Echo), "value").DefaultTimeout();
        Assert.Null(result.Error);
        Assert.Equal("value", result.Result);

        client.Dispose();
        await connectionTask.DefaultTimeout();
    }
}
