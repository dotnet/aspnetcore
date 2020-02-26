// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [/*MM*/Route("/mypage")]
    public class DiagnosticsAreReturned_IfRouteAttribute_IsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
