// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_ForApiController_IfConditionalExpressionReturnsUndocumentedStatusCodes : ControllerBase
    {
        [ProducesResponseType(201)]
        public IActionResult Method(int id)
        {
            return id == 0 ? /*MM*/(IActionResult)NotFound() : Ok();
        }
    }
}
