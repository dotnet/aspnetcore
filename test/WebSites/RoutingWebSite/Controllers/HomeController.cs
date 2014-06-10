// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RoutingWebSite
{
    // This controller is reachable via traditional routing.
    public class HomeController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public HomeController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult Index()
        {
            return _generator.Generate("/", "/Home", "/Home/Index");
        }

        public IActionResult About()
        {
            // There are no urls that reach this action - it's hidden by an attribute route.
            return _generator.Generate();
        }
    }
}