// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests;

public partial class HubConnectionHandlerTests
{
    [Fact]
    public async Task HubMethodAuthorizationUsesScopedServiceAndCurrentResourceOnEveryInvocation()
    {
        using var log = StartVerifiableLog();
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var policyProvider = new Mock<IAuthorizationPolicyProvider>();
        policyProvider.Setup(p => p.AllowsCachingPolicies).Returns(true);
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
        var second = await client.InvokeAsync(nameof(MethodHub.MultiParamAuthMethod), "second", "value").DefaultTimeout();
        Assert.NotNull(second.Error);

        Assert.Equal(2, authorizationServices.Count);
        Assert.All(authorizationServices, service => service.Verify(s => s.AuthorizeAsync(
            It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()), Times.Once));
        Assert.All(principals, principal => Assert.Same(client.Connection.User, principal));
        Assert.Equal(2, resources.Count);
        Assert.Equal(new object[] { "first", "value" }, resources[0].HubMethodArguments);
        Assert.Equal(new object[] { "second", "value" }, resources[1].HubMethodArguments);
        Assert.NotSame(resources[0], resources[1]);
        Assert.NotSame(resources[0].ServiceProvider, resources[1].ServiceProvider);
        Assert.NotSame(resources[0].Hub, resources[1].Hub);
        policyProvider.Verify(p => p.GetPolicyAsync("test"), Times.Once);

        client.Dispose();
        await connectionTask.DefaultTimeout();
    }
}
