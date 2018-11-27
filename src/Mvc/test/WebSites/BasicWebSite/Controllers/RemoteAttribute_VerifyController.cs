// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class RemoteAttribute_VerifyController : Controller
    {
        // This action is overloaded and may receive requests to validate UserId1, UserId2 or UserId5.
        [AcceptVerbs("Get", "Post")]
        [Route("[controller]/[action]", Name = "VerifyRoute")]
        public IActionResult IsIdAvailable(string userId1, string userId2, string userId5)
        {
            string name;
            string value;
            if (userId1 != null)
            {
                name = nameof(RemoteAttributeUser.UserId1);
                value = userId1;
            }
            else if (userId2 != null)
            {

                name = nameof(RemoteAttributeUser.UserId2);
                value = userId2;
            }
            else if (userId5 != null)
            {

                name = nameof(RemoteAttributeUser.UserId5);
                value = userId5;
            }
            else
            {
                name = "unknown";
                value = string.Empty;
            }

            return Json(data: $"/RemoteAttribute_Verify/IsIdAvailable rejects {name}: '{value}'.");
        }
    }
}