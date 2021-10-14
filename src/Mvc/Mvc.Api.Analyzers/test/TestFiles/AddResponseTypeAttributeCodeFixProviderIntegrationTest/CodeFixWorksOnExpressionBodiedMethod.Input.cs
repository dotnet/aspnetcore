namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWorksOnExpressionBodiedMethodController : ControllerBase
    {
        public IActionResult GetItem() => NotFound();
    }
}
