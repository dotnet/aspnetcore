// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using RazorWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace RazorWebSite.Controllers
{
    public class EnumController : Controller
    {
        public IActionResult Enum()
        {
            return View(new EnumModel{ Id = ModelEnum.FirstOption });
        }
    }
}
