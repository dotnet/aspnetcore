namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterName : Controller
    {
        [HttpPost]
        public IActionResult Edit([ModelBinder(Name = "model")] DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterNameModel /*MM*/parameter) => null;
    }

    public class DiagnosticsAreReturned_IfModelNameProviderIsUsedToModifyParameterNameModel
    {
        public string Model { get; }

        public string Value { get; }
    }
}
