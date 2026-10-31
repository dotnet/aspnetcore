// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace CustomPolicyProvider.Controllers;

// Sample actions to demonstrate the use of the [MinimumAgeAuthorize] attribute
[Controller]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // View protected with custom parameterized authorization policy
    [MinimumAgeAuthorize(10)]
    public IActionResult MinimumAge10()
    {
        return View("MinimumAge", 10);
    }

    // View protected with custom parameterized authorization policy
    [MinimumAgeAuthorize(50)]
    public IActionResult MinimumAge50()
    {
        return View("MinimumAge", 50);
    }
}
