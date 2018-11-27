namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode : ControllerBase
    {
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult Method(int id)
        {
            if (id == 0)
            {
                /*MM*/return NotFound();
            }

            return Ok();
        }
    }
}
