// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    [ApiController]
    public class NoDiagnosticsAreReturnedForApiControllers : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(NoDiagnosticsAreReturnedForApiControllersModel model) => null;
    }

    public class NoDiagnosticsAreReturnedForApiControllersModel
    {
        public string Model { get; }
    }
}
