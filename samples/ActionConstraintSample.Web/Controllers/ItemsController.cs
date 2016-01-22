// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ActionConstraintSample.Web.Controllers
{
    public class ItemsController : Controller
    {
        public IActionResult GetItems()
        {
            return Content("Hello from everywhere");
        }
    }
}