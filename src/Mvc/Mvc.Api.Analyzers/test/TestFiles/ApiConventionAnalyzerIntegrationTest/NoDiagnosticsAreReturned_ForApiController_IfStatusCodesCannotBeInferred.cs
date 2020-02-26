// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class NoDiagnosticsAreReturned_ForApiController_IfStatusCodesCannotBeInferred : ControllerBase
    {
        [ProducesResponseType(201)]
        public IActionResult Method(int id)
        {
            return id == 0 ? (IActionResult)NotFound() : Ok();
        }
    }
}
