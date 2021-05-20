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
