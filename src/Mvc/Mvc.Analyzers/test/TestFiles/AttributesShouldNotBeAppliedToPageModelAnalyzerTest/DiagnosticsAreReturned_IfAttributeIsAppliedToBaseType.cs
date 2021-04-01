// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public abstract class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
        [/*MM*/Authorize]
        public void OnGet() { }
    }

    public class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseType : DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
    }
}
