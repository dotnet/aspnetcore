// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
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
}
