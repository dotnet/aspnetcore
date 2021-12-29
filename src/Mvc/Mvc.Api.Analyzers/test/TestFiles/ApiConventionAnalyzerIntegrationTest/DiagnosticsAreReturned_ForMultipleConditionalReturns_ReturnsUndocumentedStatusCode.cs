// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_ForMultipleConditionalReturns_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Get(int id)
        {
            return /*MM*/id == 0
                ? NotFound()
                : id == 1
                    ? BadRequest()
                    : Ok();
        }
    }
}
