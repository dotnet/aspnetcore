// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProviderTest._INPUT_
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CodeFixRemovesModelStateIsInvalidBlockWithEqualityCheck : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }

            if (ModelState.IsValid == false)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
