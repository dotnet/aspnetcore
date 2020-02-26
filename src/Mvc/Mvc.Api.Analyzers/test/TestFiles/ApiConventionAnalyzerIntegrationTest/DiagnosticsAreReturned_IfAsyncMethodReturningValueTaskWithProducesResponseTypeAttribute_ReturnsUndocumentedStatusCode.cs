// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfAsyncMethodReturningValueTaskWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(typeof(string), 404)]
        public async ValueTask<IActionResult> Method(int id)
        {
            await Task.Yield();
            if (id == 0)
            {
                return NotFound();
            }

            /*MM*/return Ok();
        }
    }
}
