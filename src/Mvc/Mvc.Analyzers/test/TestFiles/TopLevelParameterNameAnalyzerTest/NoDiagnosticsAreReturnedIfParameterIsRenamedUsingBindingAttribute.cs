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
