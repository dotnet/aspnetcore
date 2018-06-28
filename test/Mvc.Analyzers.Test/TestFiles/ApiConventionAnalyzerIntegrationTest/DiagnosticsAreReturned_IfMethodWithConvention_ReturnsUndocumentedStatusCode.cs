using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithConvention_ReturnsUndocumentedStatusCode : ControllerBase
    {
        public IActionResult Get(int id)
        {
            if (id < 0)
            {
                /*MM*/return BadRequest();
            }

            if (id == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
