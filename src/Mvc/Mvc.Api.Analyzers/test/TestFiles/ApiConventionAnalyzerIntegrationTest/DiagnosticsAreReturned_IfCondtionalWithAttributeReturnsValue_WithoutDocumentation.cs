// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfCondtionalWithAttributeReturnsValue_WithoutDocumentation : ControllerBase
    {
        [ProducesResponseType(404)]
        public ActionResult<DiagnosticsAreReturned_IfCondtionalWithAttributeReturnsValue_WithoutDocumentationBaseModel> Get(int id)
        {
            return /*MM*/id == 0 ? NotFound() : new DiagnosticsAreReturned_IfCondtionalWithAttributeReturnsValue_WithoutDocumentationBaseModel();
        }
    }

    public class DiagnosticsAreReturned_IfCondtionalWithAttributeReturnsValue_WithoutDocumentationBaseModel { }
}
