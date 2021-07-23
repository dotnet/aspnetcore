namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsNumericLiteralForNonExistingStatusCodeConstantsController : ControllerBase
    {
        public IActionResult GetItem(int id)
        {
            if (id == 0)
            {
                return StatusCode(345);
            }

            return Ok(new object());
        }
    }
}
