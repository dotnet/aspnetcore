// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class Home : PageModel
    {
#pragma warning disable MVC1001
        [ProducesResponseType(302)]
        public IActionResult OnPost(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Page();
        }
#pragma warning restore MVC1001
    }
}
