// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Security;

public class AuthorizationPolicyBenchmark
{
    private DefaultAuthorizationPolicyProvider _policyProvider;

    [GlobalSetup]
    public void Setup()
    {
        _policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
    }

    [Benchmark]
    public Task CombineAsync()
    {
        return AuthorizationPolicy.CombineAsync(_policyProvider, Array.Empty<IAuthorizeData>());
    }
}
