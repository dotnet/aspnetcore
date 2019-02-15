namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    [ApiController]
    [Route("/api/[controller]")]
    public class NoDiagnosticsAreReturned_ForApiActionsCheckingAdditionalConditions : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (!ModelState.IsValid && !Request.Query.ContainsKey("skip-validation"))
            {
                return UnprocessableEntity();
            }

            return Ok();
        }
    }
}
