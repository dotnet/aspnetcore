// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    [ApiController]
    [Route("/api/[controller]")]
    public class NoDiagnosticsAreReturned_ForApiActionsReturningNot400FromNonModelStateIsValidBlock : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity();
            }

            return Ok();
        }
    }
}
