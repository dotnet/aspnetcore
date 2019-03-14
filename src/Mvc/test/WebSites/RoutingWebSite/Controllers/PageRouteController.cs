// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite
{
    public class PageRouteController
    {
        private readonly TestResponseGenerator _generator;

        public PageRouteController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        public IActionResult ConventionalRoute(string page)
        {
            return _generator.Generate("/PageRoute/ConventionalRoute/" + page);
        }

        [HttpGet("/PageRoute/Attribute/{page}")]
        public IActionResult AttributeRoute(string page)
        {
            return _generator.Generate("/PageRoute/Attribute/" + page);
        }
    }
}
