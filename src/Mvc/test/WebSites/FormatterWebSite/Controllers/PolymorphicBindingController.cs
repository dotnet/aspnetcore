// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers;

public class PolymorphicBindingController : ControllerBase
{
    public IActionResult ModelBound([ModelBinder(typeof(PolymorphicBinder))] BaseModel person)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok(person);
    }

    [HttpPost]
    public IActionResult InputFormatted([FromBody] IModel person)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok(person);
    }
}
