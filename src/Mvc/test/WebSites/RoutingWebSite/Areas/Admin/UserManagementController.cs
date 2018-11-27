// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace RoutingWebSite.Admin
{
    [Area("Admin")]
    [Route("[area]/Users")]
    public class UserManagementController : Controller
    {
        private readonly TestResponseGenerator _generator;

        public UserManagementController(TestResponseGenerator generator)
        {
            _generator = generator;
        }

        [HttpGet("All")]
        public IActionResult ListUsers()
        {
            return _generator.Generate("Admin/Users/All");
        }
    }
}