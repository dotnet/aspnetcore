namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProviderTest._INPUT_
{
    [ApiController]
    [Route("/api/[controller]")]
    public class CodeFixRemovesIfBlockWithoutBraces : ControllerBase
    {
        public IActionResult Method(int id)
        {
            if (id == 0)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok();
        }
    }
}
