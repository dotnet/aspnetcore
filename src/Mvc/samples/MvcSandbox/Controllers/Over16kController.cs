// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    public class Over16kController : Controller
    {
        [ModelBinder]
        public string Id { get; set; }

        public IActionResult Index()
        {
            return View();
        }
    }
}
