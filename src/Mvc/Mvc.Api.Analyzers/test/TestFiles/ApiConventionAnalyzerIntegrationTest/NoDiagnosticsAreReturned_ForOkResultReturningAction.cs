// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class NoDiagnosticsAreReturned_ForOkResultReturningAction : ControllerBase
    {
        public async Task<ActionResult<IEnumerable<NoDiagnosticsAreReturned_ForOkResultReturningAction>>> Action()
        {
            await Task.Yield();
            var models = new List<NoDiagnosticsAreReturned_ForOkResultReturningActionModel>();

            return Ok(models);
        }
    }

    public class NoDiagnosticsAreReturned_ForOkResultReturningActionModel { }
}
