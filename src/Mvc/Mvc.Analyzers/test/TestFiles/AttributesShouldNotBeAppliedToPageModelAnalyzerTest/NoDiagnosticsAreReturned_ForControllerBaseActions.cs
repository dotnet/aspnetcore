// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
