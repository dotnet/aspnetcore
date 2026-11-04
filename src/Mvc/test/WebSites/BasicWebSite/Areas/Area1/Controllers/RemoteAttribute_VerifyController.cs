// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Areas.Area1.Controllers;

[Area("Area1")]
[Route("[area]/[controller]/[action]")]
public class RemoteAttribute_VerifyController : Controller
{
    // This action is overloaded and may receive requests to validate either UserId1 or UserId3.
    // Demonstrates use of the default error message.
    [AcceptVerbs("Get", "Post")]
    public IActionResult IsIdAvailable(string userId1, string userId3)
    {
        return new JsonResult(value: false);
    }
}
