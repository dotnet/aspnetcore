// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace CompositeViewEngineWebSite
{
    public class HomeController : Controller
    {
        public ViewResult Index()
        {
            return View();
        }

        public ViewResult TestView()
        {
            return View("test-view");
        }
    }
}