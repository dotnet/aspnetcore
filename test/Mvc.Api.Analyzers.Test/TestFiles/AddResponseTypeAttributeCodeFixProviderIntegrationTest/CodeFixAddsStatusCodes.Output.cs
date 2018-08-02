namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsStatusCodesController : ControllerBase
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesDefaultResponseType]
        public IActionResult GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Ok(new object());
        }
    }
}
