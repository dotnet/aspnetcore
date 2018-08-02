using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsSuccessStatusCode : ControllerBase
    {
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesDefaultResponseType]
        public ActionResult<object> GetItem(string id)
        {
            if (!int.TryParse(id, out var idInt))
            {
                return BadRequest();
            }

            if (idInt == 0)
            {
                return NotFound();
            }

            return Created("url", new object());
        }
    }
}
