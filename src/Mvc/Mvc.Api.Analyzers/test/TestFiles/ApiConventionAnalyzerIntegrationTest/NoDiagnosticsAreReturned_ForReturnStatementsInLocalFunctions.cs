// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
