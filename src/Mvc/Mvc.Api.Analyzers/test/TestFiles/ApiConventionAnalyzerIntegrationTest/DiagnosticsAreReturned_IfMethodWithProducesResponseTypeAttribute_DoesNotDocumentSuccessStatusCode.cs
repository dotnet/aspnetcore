// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_DoesNotDocumentSuccessStatusCode : ControllerBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public ActionResult<string> /*MM*/Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            throw new NotImplementedException();
        }
    }
}
