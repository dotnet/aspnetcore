// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

public class SerializableErrorController : Controller
{
    [HttpPost]
    public IActionResult CreateEmployee([FromBody] Employee employee)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Content("Hello World!");
    }
}
