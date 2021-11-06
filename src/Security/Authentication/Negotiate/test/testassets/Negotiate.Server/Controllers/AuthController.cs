// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Negotiate.Server.Controllers;

[Route("auth")]
[ApiController]
public class AuthController : ControllerBase
{
    [HttpGet]
    [Route("Unrestricted")]
    public ObjectResult GetUnrestricted()
    {
        var user = HttpContext.User.Identity;
        return new ObjectResult(new
        {
            user.Name,
            user.AuthenticationType,
        });
    }

    [HttpGet]
    [Authorize]
    [Route("Authorized")]
    public ObjectResult GetAuthorized()
    {
        var user = HttpContext.User.Identity;
        return new ObjectResult(new
        {
            user.Name,
            user.AuthenticationType,
        });
    }

    [HttpGet]
    [Authorize]
    [Route("Unauthorized")]
    public ChallengeResult GetUnauthorized()
    {
        return Challenge();
    }
}
