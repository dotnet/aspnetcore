// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ProducesErrorResponseType(typeof(CodeFixAddsResponseTypeWhenDifferentErrorModel))]
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsResponseTypeWhenDifferentFromErrorType : ControllerBase
    {
        public IActionResult GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound(new CodeFixAddsResponseTypeWhenDifferentErrorModel());
            }

            if (id == 1)
            {
                var validationProblemDetails = new ValidationProblemDetails(ModelState);
                return BadRequest(validationProblemDetails);
            }

            return Ok(new object());
        }
    }

    public class CodeFixAddsResponseTypeWhenDifferentErrorModel { }
}
