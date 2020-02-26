// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class NoDiagnosticsAreReturnedForNonActions : Controller
    {
        [NonAction]
        public IActionResult EditPerson(NoDiagnosticsAreReturnedForNonActionsModel model) => null;
    }

    public class NoDiagnosticsAreReturnedForNonActionsModel
    {
        public string Model { get; }
    }
}
