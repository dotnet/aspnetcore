// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class NoDiagnosticsAreReturned_ForApiController_IfStatusCodesCannotBeInferred : ControllerBase
    {
        [ProducesResponseType(201)]
        public IActionResult Method(int id)
        {
            IActionResult result = id == 0 ? NotFound() : Ok();
            return result;
        }
    }
}
