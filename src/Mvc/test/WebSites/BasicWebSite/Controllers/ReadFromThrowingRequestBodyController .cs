// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers;

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
