// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization
{
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
}
