// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
