// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace AutofacWebSite.Controllers
{
    public class DIController : Controller
    {
        public DIController(HelloWorldBuilder builder)
        {
            Builder = builder;
        }

        public HelloWorldBuilder Builder { get; private set; }

        public IActionResult Index()
        {
            return View(model: Builder.Build());
        }
    }
}
