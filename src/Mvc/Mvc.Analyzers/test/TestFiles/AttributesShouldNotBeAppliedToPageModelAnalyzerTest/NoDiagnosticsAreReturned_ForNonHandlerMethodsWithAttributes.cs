// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForNonHandlerMethodsWithAttributes : PageModel
    {
        [Authorize]
        private void OnGetPrivate() { }

        [TypeFilter(typeof(object))]
        internal IActionResult OnPost() => null;

        [AllowAnonymous]
        public void OnGet<T>() { }

        [ServiceFilter(typeof(object))]
        public static void OnPostStatic() { }
    }
}
