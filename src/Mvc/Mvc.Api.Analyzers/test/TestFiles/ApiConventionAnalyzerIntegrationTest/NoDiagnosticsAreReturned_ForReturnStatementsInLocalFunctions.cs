// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class NoDiagnosticsAreReturned_ForReturnStatementsInLocalFunctions : ControllerBase
    {
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 404)]
        public IActionResult Put(int id, object model)
        {
            if (id == 0)
            {
                return NotFound();
            }

            if (id == 1)
            {
                return LocalFunction();
            }

            return Ok();

            IActionResult LocalFunction()
            {
                if (id < -1)
                {
                    // We should not process this.
                    return UnprocessableEntity();
                }

                return null;
            }
        }
    }
}
