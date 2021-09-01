// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForControllerBaseActions : ControllerBase
    {
        [Authorize]
        public IActionResult AuthorizeAttribute() => null;

        [ServiceFilter(typeof(object))]
        public IActionResult ServiceFilter() => null;
    }
}
