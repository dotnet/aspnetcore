// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace RazorPagesWebSite;

[Area("Accounts")]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
