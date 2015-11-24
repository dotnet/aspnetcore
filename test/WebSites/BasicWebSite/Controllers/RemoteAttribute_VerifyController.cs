// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace BasicWebSite.Controllers
{
    [Route("[controller]/[action]")]
    public class RemoteAttribute_VerifyController : Controller
    {
        // This action is overloaded and may receive requests to validate either UserId1 or UserId2.
        [AcceptVerbs("Get", "Post")]
        public IActionResult IsIdAvailable(string userId1, string userId2)
        {
            return Json(data: string.Format("/RemoteAttribute_Verify/IsIdAvailable rejects {0}.", userId1 ?? userId2));
        }
    }
}