// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfAsyncMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> Method(int id)
        {
            await Task.Yield();
            if (id == 0)
            {
                return /*MM*/NotFound();
            }

            return Ok();
        }
    }
}
