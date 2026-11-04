// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Ares.Area2.Controllers;

[Area("Area2")]
[Route("[area]/[controller]/[action]")]
public class RemoteAttribute_VerifyController : Controller
{
    // Demonstrates validation action when AdditionalFields causes client to send multiple values.
    [HttpGet]
    public IActionResult IsIdAvailable(RemoteAttributeUser user)
    {
        return new JsonResult(value: string.Format(
            CultureInfo.InvariantCulture,
            "/Area2/RemoteAttribute_Verify/IsIdAvailable rejects '{0}' with '{1}', '{2}', and '{3}'.",
            user.UserId4,
            user.UserId1,
            user.UserId2,
            user.UserId3));
    }
}
