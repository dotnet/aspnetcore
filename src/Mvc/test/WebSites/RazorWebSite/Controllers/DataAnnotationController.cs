// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using RazorWebSite.Models;

namespace RazorWebSite.Controllers;

public class EnumController : Controller
{
    public IActionResult Enum()
    {
        return View(new EnumModel { Id = ModelEnum.FirstOption });
    }
}
