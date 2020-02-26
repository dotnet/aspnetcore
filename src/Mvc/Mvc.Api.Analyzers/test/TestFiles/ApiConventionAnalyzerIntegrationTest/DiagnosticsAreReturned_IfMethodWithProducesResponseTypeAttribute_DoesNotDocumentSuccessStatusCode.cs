// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
