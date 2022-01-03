// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization;

public class TestAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions options = new AuthorizationOptions();

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => Task.FromResult(options.DefaultPolicy);

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
        => Task.FromResult(options.FallbackPolicy);

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName) => Task.FromResult(
        new AuthorizationPolicy(new[]
        {
                    new TestPolicyRequirement { PolicyName = policyName }
        },
        new[] { $"TestScheme:{policyName}" }));
}

public class TestPolicyRequirement : IAuthorizationRequirement
{
    public string PolicyName { get; set; }
}
