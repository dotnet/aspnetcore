// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class DiagnosticsAreReturned_ForModelBoundParameters : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(
            [FromBody] DiagnosticsAreReturned_ForModelBoundParametersModel model,
            [FromQuery] DiagnosticsAreReturned_ForModelBoundParametersModel /*MM*/value) => null;
    }

    public class DiagnosticsAreReturned_ForModelBoundParametersModel
    {
        public string Model { get; }

        public string Value { get; }
    }
}
