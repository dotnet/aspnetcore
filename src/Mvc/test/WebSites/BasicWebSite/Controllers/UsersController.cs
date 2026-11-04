// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

public class UsersController : Controller
{
    public IActionResult Index()
    {
        return Content("Users.Index");
    }
}
