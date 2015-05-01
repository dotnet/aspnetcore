// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ValidationWebSite.MyArea.Controllers
{
    [Area("Aria")]
    public class RemoteAttribute_VerifyController : Controller
    {
        // This action is overloaded and may receive requests to validate either UserId1 or UserId3.
        // Demonstrates use of the default error message.
        [AcceptVerbs("Get", "Post")]
        [Route("[Area]/[Controller]/[Action]", Order = -2)]
        public IActionResult IsIdAvailable(string userId1, string userId3)
        {
            return Json(data: false);
        }
    }
}