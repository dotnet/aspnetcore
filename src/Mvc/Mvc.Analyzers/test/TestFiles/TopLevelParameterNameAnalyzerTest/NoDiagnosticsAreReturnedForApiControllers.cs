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
