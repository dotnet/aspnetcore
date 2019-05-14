using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfAsyncMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(typeof(string), 200)]
        public async Task<IActionResult> Method(int id)
        {
            await Task.Yield();
            if (id == 0)
            {
                /*MM*/return NotFound();
            }

            return Ok();
        }
    }
}
