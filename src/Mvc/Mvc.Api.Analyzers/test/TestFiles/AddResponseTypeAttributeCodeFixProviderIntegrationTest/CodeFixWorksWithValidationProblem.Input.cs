// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWorksWithValidationProblem : ControllerBase
    {
        public IActionResult GetProblem()
        {
            return ValidationProblem();
        }
    }
}
