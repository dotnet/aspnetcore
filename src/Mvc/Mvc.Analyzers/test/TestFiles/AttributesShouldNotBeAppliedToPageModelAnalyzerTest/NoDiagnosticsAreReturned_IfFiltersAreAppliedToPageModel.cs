// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [ServiceFilter(typeof(object))]
    public class NoDiagnosticsAreReturned_IfFiltersAreAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
