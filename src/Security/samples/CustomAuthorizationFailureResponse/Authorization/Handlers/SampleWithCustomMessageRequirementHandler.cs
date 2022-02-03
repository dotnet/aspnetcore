// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using CustomAuthorizationFailureResponse.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace CustomAuthorizationFailureResponse.Authorization.Handlers;

public class SampleWithCustomMessageRequirementHandler : AuthorizationHandler<SampleWithCustomMessageRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SampleWithCustomMessageRequirement requirement)
    {
        // assuming the requirement was not met
        return Task.CompletedTask;
    }
}
