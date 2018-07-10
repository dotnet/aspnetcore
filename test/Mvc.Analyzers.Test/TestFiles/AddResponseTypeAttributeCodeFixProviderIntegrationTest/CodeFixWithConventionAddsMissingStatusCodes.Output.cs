using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWithConventionAddsMissingStatusCodes : ControllerBase
    {
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
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
