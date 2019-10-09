using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class NoDiagnosticsAreReturned_ForNonApiController : Controller
    {
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
