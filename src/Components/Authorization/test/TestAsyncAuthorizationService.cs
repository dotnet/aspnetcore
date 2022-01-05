// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization;

public class TestAsyncAuthorizationService : IAuthorizationService
{
    public AuthorizationResult NextResult { get; set; }
        = AuthorizationResult.Failed();

    public List<(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)> AuthorizeCalls { get; }
        = new List<(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)>();

    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        AuthorizeCalls.Add((user, resource, requirements));

        // Make Authorization run asynchronously
        await Task.Yield();

        // The TestAuthorizationService doesn't actually apply any authorization requirements
        // It just returns the specified NextResult, since we're not trying to test the logic
        // in DefaultAuthorizationService or similar here. So it's up to tests to set a desired
        // NextResult and assert that the expected criteria were passed by inspecting AuthorizeCalls.
        return NextResult;
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
        => throw new NotImplementedException();

}
