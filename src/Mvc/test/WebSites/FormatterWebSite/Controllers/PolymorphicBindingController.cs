// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
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
}
