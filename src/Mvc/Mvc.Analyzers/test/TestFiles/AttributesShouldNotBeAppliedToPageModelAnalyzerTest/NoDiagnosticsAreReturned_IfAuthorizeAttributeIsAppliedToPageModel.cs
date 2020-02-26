// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [Authorize]
    public class NoDiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
