// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using CustomAuthorizationFailureResponse.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace CustomAuthorizationFailureResponse.Authorization.Handlers;

public class SampleWithFailureReasonRequirementHandler : AuthorizationHandler<SampleFailReasonRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SampleFailReasonRequirement requirement)
    {
        context.Fail(new AuthorizationFailureReason(this, "This is a way to provide more failure reasons."));
        return Task.CompletedTask;
    }
}
