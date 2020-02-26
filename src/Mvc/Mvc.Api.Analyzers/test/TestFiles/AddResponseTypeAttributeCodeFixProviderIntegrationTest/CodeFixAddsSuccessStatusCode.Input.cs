// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsSuccessStatusCode : ControllerBase
    {
        public ActionResult<object> GetItem(string id)
        {
            if (!int.TryParse(id, out var idInt))
            {
                return BadRequest();
            }

            if (idInt == 0)
            {
                return NotFound();
            }

            return Created("url", new object());
        }
    }
}
