// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            return /*MM*/new DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentationModel();
        }
    }

    public class DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentationModel { }
}
