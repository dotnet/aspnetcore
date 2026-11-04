// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Admin;

[Area("Admin")]
[Route("[area]/Users")]
public class UserManagementController : Controller
{
    private readonly TestResponseGenerator _generator;

    public UserManagementController(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [HttpGet("All")]
    public IActionResult ListUsers()
    {
        return _generator.Generate("Admin/Users/All");
    }
}
