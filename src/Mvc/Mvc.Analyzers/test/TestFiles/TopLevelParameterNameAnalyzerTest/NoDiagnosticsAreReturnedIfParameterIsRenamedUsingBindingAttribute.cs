// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttribute : Controller
    {
        [HttpPost]
        public IActionResult EditPerson([FromForm(Name = "")] NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttributeModel model) => null;
    }

    public class NoDiagnosticsAreReturnedIfParameterIsRenamedUsingBindingAttributeModel
    {
        public string Model { get; }
    }
}
