// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ValidationWebSite.Models;

namespace ValidationWebSite.AnotherArea.Controllers
{
    [Area("AnotherAria")]
    [Route("[Area]/[Controller]/[Action]", Order = -2)]
    public class RemoteAttribute_VerifyController : Controller
    {
        // Demonstrates validation action when AdditionalFields causes client to send multiple values.
        [HttpGet]
        public IActionResult IsIdAvailable(Person person)
        {
            return Json(data: string.Format(
                "/AnotherAria/RemoteAttribute_Verify/IsIdAvailable rejects '{0}' with '{1}', '{2}', and '{3}'.",
                person.UserId4,
                person.UserId1,
                person.UserId2,
                person.UserId3));
        }
    }
}