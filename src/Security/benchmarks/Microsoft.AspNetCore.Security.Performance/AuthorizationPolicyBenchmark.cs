// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Security
{
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
}
