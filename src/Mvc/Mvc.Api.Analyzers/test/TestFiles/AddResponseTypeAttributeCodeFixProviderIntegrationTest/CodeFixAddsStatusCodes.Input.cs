namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsStatusCodesController : ControllerBase
    {
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
