// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentation : ControllerBase
    {
        [ProducesResponseType(404)]
        public async Task<ActionResult<DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentationModel>> Method(int id)
        {
            await Task.Yield();

            if (id == 0)
            {
                return NotFound();
            }

            return /*MM*/new DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentationModel();
        }
    }

    public class DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentationModel { }
}
