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
