// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfRouteAttributesAreAppliedToPageHandlerMethod : PageModel
    {
        [/*MM*/HttpHead]
        public void OnGet()
        {
        }
    }
}
