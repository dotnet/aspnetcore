// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class DefaultOutputCachePolicyProviderTests
{
    [Theory]
    [InlineData("")]
    [InlineData("policyName")]
    public async Task GetsNamedPolicy(string policyName)
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy = DefaultPolicy.Instance;
        options.AddPolicy(policyName, policy);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync(policyName);

        // Assert
        Assert.Same(policy, actualPolicy);
    }
}