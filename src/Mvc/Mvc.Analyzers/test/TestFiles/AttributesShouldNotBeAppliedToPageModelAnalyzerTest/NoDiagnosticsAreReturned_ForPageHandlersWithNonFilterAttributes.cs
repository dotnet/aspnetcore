// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
