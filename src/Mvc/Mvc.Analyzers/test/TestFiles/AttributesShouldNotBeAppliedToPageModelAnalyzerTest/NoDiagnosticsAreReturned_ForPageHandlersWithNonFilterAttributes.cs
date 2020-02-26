// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForPageHandlersWithNonFilterAttributes : PageModel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnGet()
        {
        }
    }
}
