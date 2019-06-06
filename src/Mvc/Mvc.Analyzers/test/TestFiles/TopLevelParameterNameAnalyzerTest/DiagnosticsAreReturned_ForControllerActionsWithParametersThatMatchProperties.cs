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
