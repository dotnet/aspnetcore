// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization
{
    public class TestAuthorizationService : IAuthorizationService
    {
        public AuthorizationResult NextResult { get; set; }
            = AuthorizationResult.Failed();

        public List<(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)> AuthorizeCalls { get; }
            = new List<(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)>();

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            AuthorizeCalls.Add((user, resource, requirements));

            // The TestAuthorizationService doesn't actually apply any authorization requirements
            // It just returns the specified NextResult, since we're not trying to test the logic
            // in DefaultAuthorizationService or similar here. So it's up to tests to set a desired
            // NextResult and assert that the expected criteria were passed by inspecting AuthorizeCalls.
            return Task.FromResult(NextResult);
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
            => throw new NotImplementedException();
    }
}
