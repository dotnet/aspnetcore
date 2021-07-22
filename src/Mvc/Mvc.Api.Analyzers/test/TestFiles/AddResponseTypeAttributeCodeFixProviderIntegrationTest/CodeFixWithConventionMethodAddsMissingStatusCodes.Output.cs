using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWithConventionMethodAddsMissingStatusCodes : ControllerBase
    {
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public ActionResult<string> GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Accepted("Result");
        }
    }
}
