// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public abstract class CustomPageModel
    {

    }

    public class DiagnosticsAreReturned_IfFiltersAreAppliedToPageHandlerMethodDerivingFromCustomModel : CustomPageModel
    {
        [/*MM*/ServiceFilter(typeof(object))]
        public void OnGet()
        {
        }
    }
}
