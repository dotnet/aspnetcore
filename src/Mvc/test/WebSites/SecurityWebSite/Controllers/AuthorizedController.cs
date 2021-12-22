// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityWebSite.Controllers;

[Authorize] // requires any authenticated user (aka the application cookie typically)
public class AuthorizedController : ControllerBase
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    public IActionResult Api() => Ok();

    public IActionResult Cookie() => Ok();

    [AllowAnonymous]
    public IActionResult AllowAnonymous() => Ok();
}
