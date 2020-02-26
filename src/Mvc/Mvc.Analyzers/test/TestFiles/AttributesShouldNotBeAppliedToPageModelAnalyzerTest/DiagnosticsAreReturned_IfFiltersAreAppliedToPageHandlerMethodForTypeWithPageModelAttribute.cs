// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public class DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodForTypeWithPageModelAttribute
    {
        [/*MM*/ServiceFilter(typeof(object))]
        public void OnGet()
        {
        }
    }
}
