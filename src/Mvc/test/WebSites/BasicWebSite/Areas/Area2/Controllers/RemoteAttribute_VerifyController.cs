// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using BasicWebSite.Models;

namespace BasicWebSite.Ares.Area2.Controllers
{
    [Area("Area2")]
    [Route("[area]/[controller]/[action]")]
    public class RemoteAttribute_VerifyController : Controller
    {
        // Demonstrates validation action when AdditionalFields causes client to send multiple values.
        [HttpGet]
        public IActionResult IsIdAvailable(RemoteAttributeUser user)
        {
            return new JsonResult(value: string.Format(
                "/Area2/RemoteAttribute_Verify/IsIdAvailable rejects '{0}' with '{1}', '{2}', and '{3}'.",
                user.UserId4,
                user.UserId1,
                user.UserId2,
                user.UserId3));
        }
    }
}