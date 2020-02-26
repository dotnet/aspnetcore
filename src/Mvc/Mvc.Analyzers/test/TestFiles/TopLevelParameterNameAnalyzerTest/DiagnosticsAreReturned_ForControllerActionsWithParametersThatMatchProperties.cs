// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchProperties : Controller
    {
        [HttpPost]
        public IActionResult EditPerson(DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchPropertiesModel /*MM*/model) => null;
    }

    public class DiagnosticsAreReturned_ForControllerActionsWithParametersThatMatchPropertiesModel
    {
        public string Model { get; }
    }
}
