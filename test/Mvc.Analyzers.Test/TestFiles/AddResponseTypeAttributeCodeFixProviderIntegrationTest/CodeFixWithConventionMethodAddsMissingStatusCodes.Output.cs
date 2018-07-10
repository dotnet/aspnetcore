namespace Microsoft.AspNetCore.Mvc.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWithConventionMethodAddsMissingStatusCodes : ControllerBase
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
