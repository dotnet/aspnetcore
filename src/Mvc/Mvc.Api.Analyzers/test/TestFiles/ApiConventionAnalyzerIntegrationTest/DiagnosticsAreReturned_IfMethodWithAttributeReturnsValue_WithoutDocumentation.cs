// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentation : ControllerBase
    {
        [ProducesResponseType(404)]
        public ActionResult<DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentationModel> Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            /*MM*/return new DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentationModel();
        }
    }

    public class DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentationModel { }
}
