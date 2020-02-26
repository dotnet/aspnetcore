// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProviderTest._OUTPUT_
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CodeFixRemovesIfBlockWithoutBraces : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 0)
                return BadRequest();
            return Ok();
        }
    }
}
