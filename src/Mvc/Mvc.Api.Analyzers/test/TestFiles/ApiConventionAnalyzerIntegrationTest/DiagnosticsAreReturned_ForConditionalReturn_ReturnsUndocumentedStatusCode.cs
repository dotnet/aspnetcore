// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_ForConditionalReturn_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(200)]
        public IActionResult Get(int id)
        {
            return /*MM*/id == 0 ? NotFound() : Ok();
        }
    }
}
