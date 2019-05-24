namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    [ApiController]
    [Route("/api/[controller]")]
    public class DiagnosticsAreReturned_ForApiActionsWithModelStateChecksUsingEquality : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 1)
            {
                return NotFound();
            }

            /*MM*/if (ModelState.IsValid == false)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
