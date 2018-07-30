namespace Microsoft.AspNetCore.Mvc.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    [ApiController]
    public class NoDiagnosticsAreReturned_ForApiActionsWithoutModelStateChecks : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
