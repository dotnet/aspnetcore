// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CustomAuthorizationFailureResponse.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomAuthorizationFailureResponse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet("customPolicyWithCustomForbiddenMessage")]
    [Authorize(Policy = SamplePolicyNames.CustomPolicyWithCustomForbiddenMessage)]
    public string GetWithCustomPolicyWithCustomForbiddenMessage()
    {
        return "Hello world from GetWithCustomPolicyWithCustomForbiddenMessage";
    }

    [HttpGet("customPolicy")]
    [Authorize(Policy = SamplePolicyNames.CustomPolicy)]
    public string GetWithCustomPolicy()
    {
        return "Hello world from GetWithCustomPolicy";
    }

    [HttpGet("failureReason")]
    [Authorize(Policy = SamplePolicyNames.FailureReasonPolicy)]
    public string FailureReason()
    {
        return "Hello world from FailureReason";
    }
}
