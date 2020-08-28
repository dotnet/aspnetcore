// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class ReadFromThrowingRequestBodyController : Controller
    {
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult AppliesAntiforgeryValidation() => Ok();

        [HttpPost]
        public IActionResult ReadForm(Person person, IFormFile form)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem();
            }

            return Ok();
        }

        public class Person
        {
            public string Name { get; set; }

            public int Age { get; set; }
        }
    }
}
