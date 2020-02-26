// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [AllowAnonymous]
    public class NoDiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
