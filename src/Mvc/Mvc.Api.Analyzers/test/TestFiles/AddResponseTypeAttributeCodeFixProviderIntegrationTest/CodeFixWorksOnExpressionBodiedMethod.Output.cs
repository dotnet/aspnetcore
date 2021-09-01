using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWorksOnExpressionBodiedMethodController : ControllerBase
    {
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetItem() => NotFound();
    }
}
