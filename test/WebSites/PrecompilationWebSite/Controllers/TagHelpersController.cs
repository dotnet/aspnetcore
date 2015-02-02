// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace PrecompilationWebSite.Controllers
{
    public class TagHelpersController : Controller
    {
        public IActionResult Add()
        {
            return View();
        }

        public IActionResult Remove()
        {
            return View();
        }
    }
}