// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

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

        return new JsonResult($"/RemoteAttribute_Verify/IsIdAvailable rejects {name}: '{value}'.");
    }
}
