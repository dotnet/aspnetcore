namespace Microsoft.AspNetCore.Mvc.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    public class NoDiagnosticsAreReturned_ForNonApiController : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
