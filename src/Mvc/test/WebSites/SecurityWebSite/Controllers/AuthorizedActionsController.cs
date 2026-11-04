// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers;

public class AuthorizedActionsController : ControllerBase
{
    [AllowAnonymous]
    public IActionResult ActionWithoutAllowAnonymous() => Ok();

    public IActionResult ActionWithoutAuthAttribute() => Ok();

    [Authorize("RequireClaimB")]
    public IActionResult ActionWithAuthAttribute() => Ok();
}
