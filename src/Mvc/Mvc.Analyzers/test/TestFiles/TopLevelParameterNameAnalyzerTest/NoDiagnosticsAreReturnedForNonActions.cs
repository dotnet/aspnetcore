// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
