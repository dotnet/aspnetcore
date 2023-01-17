// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authorization.Test;

public class PassThroughAuthorizationHandlerTests
{
    private IAuthorizationService BuildAuthorizationService(Action<IServiceCollection> setupServices = null)
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddLogging();
        services.AddOptions();
        setupServices?.Invoke(services);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PassThroughShouldInvokeAllHandlersBasedOnSetting(bool invokeAllHandlers)
    {
        // Arrange
        var willFail = new SelfRequirement(fail: true);
        var afterHandler = new SelfRequirement(fail: false);
        var authorizationService = BuildAuthorizationService(services =>
        {
            services.AddAuthorization(options =>
            {
                options.InvokeHandlersAfterFailure = invokeAllHandlers;
                options.AddPolicy("Self", policy => policy.Requirements.Add(willFail));
            });
            services.AddSingleton<IAuthorizationHandler>(afterHandler);
        });

        // Act
        var allowed = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), "Self");

        // Assert
        Assert.False(allowed.Succeeded);
        Assert.True(willFail.Invoked);
        Assert.Equal(invokeAllHandlers, afterHandler.Invoked);
    }

    public class SelfRequirement : AuthorizationHandler<SelfRequirement>, IAuthorizationRequirement
    {
        private readonly bool _fail;

        public SelfRequirement(bool fail) => _fail = fail;

        public bool Invoked { get; set; }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SelfRequirement requirement)
        {
            Invoked = true;
            if (_fail)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }
            return Task.FromResult(0);
        }
    }

}
