// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

public class PolymorphicPropertyBindingController : ControllerBase
{
    [FromBody]
    public IModel Person { get; set; }

    [HttpPost]
    public IActionResult Action()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok(Person);
    }
}
