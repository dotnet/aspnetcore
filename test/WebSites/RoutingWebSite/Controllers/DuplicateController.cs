// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace RoutingWebSite
{
    public class DuplicateController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public DuplicateController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet("api/Duplicate/", Name = "DuplicateRoute")]
        public ActionResult DuplicateAttribute()
        {
            return _generator.Generate(Url.RouteUrl("DuplicateRoute"));
        }

        [HttpGet("api/Duplicate/IndexAttribute")]
        public ActionResult IndexAttribute()
        {
            return _generator.Generate(Url.RouteUrl("DuplicateRoute"));
        }

        [HttpGet]
        public ActionResult Duplicate()
        {
            return _generator.Generate(Url.RouteUrl("DuplicateRoute"));
        }

        public ActionResult Index()
        {
            return _generator.Generate(Url.RouteUrl("DuplicateRoute"));
        }
    }
}