// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class NoDiagnosticsAreReturned_ForNonApiController : Controller
    {
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
