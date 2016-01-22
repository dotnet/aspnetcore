// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace InlineConstraintSample.Web.Controllers
{
    public class UsersController : Controller
    {
        [Route("constant-prefix/[controller]")]
        public IActionResult Index()
        {
            return Content("Users.Index");
        }
    }
}