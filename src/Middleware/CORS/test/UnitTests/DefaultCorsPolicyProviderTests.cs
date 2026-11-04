// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class DefaultPolicyProviderTests
{
    [Fact]
    public async Task UsesTheDefaultPolicyName()
    {
        // Arrange
        var options = new CorsOptions();
        var policy = new CorsPolicy();
        options.AddPolicy(options.DefaultPolicyName, policy);

        var corsOptions = Options.Create(options);
        var policyProvider = new DefaultCorsPolicyProvider(corsOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync(new DefaultHttpContext(), policyName: null);

        // Assert
        Assert.Same(policy, actualPolicy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("policyName")]
    public async Task GetsNamedPolicy(string policyName)
    {
        // Arrange
        var options = new CorsOptions();
        var policy = new CorsPolicy();
        options.AddPolicy(policyName, policy);

        var corsOptions = Options.Create(options);
        var policyProvider = new DefaultCorsPolicyProvider(corsOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync(new DefaultHttpContext(), policyName);

        // Assert
        Assert.Same(policy, actualPolicy);
    }
}
