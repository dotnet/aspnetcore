// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization.Test;

public class AuthorizationBuilderTests
{
    [Fact]
    public void CanSetFallbackPolicy()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var builder = TestHelpers.CreateAuthorizationBuilder()
        // Act
            .SetFallbackPolicy(policy);

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        Assert.Equal(policy, options.FallbackPolicy);
    }

    [Fact]
    public void CanUnSetFallbackPolicy()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var builder = TestHelpers.CreateAuthorizationBuilder()
            .SetFallbackPolicy(policy)
        // Act
            .SetFallbackPolicy(null);

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        Assert.Null(options.FallbackPolicy);
    }

    [Fact]
    public void CanSetDefaultPolicy()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var builder = TestHelpers.CreateAuthorizationBuilder()
        // Act
            .SetDefaultPolicy(policy);

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        Assert.Equal(policy, options.DefaultPolicy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanSetInvokeHandlersAfterFailure(bool invoke)
    {
        // Arrange
        var builder = TestHelpers.CreateAuthorizationBuilder()
        // Act
            .SetInvokeHandlersAfterFailure(invoke);

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        Assert.Equal(invoke, options.InvokeHandlersAfterFailure);
    }

    [Fact]
    public void CanAddPolicyInstance()
    {
        // Arrange
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var builder = TestHelpers.CreateAuthorizationBuilder()
        // Act
            .AddPolicy("name", policy);

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        Assert.Equal(policy, options.GetPolicy("name"));
    }

    [Fact]
    public void CanAddPolicyDelegate()
    {
        // Arrange
        var builder = TestHelpers.CreateAuthorizationBuilder()
        // Act
            .AddPolicy("name", p => p.RequireAssertion(_ => true));

        var options = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        // Assert
        var policy = options.GetPolicy("name");
        Assert.NotNull(policy);
        Assert.Single(policy.Requirements);
        Assert.IsType<AssertionRequirement>(policy.Requirements.First());
    }
}

internal class TestHelpers
{
    public static AuthorizationBuilder CreateAuthorizationBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        return services.AddAuthorizationBuilder();
    }
}
