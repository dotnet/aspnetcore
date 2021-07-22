using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsNumericLiteralForNonExistingStatusCodeConstantsController : ControllerBase
    {
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(345)]
        [ProducesDefaultResponseType]
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
