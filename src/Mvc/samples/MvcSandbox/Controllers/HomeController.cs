// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace MvcSandbox.Controllers
{
    [ApiController]
    public class HomeController : Controller
    {
        [HttpPost("/")]
        public IActionResult Index(Person person)
        {
            return Ok(person);
        }
    }

    public class Person
    {
        public int Id { get; set; }
    }

}
