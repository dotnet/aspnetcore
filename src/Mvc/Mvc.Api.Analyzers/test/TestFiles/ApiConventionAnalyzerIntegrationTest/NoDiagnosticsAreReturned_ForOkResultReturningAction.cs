// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
