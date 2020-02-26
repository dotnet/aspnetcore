// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsStatusCodesFromMethodParametersController : ControllerBase
    {
        private const int FieldStatusCode = 201;

        public IActionResult GetItem(int id)
        {
            if (id == 0)
            {
                return StatusCode(422);
            }

            if (id == 1)
            {
                return StatusCode(StatusCodes.Status202Accepted);
            }

            if (id == 2)
            {
                const int localStatusCode = 204;

                return StatusCode(localStatusCode);
            }

            if (id == 3)
            {
                return StatusCode(FieldStatusCode);
            }

            return Ok(new object());
        }
    }
}
