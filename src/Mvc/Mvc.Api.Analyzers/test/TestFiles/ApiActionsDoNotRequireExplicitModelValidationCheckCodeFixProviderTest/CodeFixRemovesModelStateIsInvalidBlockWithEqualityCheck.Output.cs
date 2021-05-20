namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProviderTest._OUTPUT_
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CodeFixRemovesModelStateIsInvalidBlockWithEqualityCheck : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
