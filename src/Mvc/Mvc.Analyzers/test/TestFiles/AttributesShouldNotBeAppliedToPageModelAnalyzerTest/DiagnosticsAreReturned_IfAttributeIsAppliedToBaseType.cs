// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
