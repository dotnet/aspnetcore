// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    public class Home : PageModel
    {
        public IActionResult OnPost(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Page();
        }
    }
}
