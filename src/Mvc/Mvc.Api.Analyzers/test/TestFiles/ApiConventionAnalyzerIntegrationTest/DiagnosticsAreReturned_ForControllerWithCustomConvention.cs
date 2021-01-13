using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Api.Analyzers;

[assembly: ApiConventionType(typeof(DiagnosticsAreReturned_ForControllerWithCustomConvention))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_ForControllerWithCustomConventionController : ControllerBase
    {
        public async Task<IActionResult> Update(int id, Product product)
        {
            if (id < 0)
            {
                /*MM*/return BadRequest();
            }

            try
            {
                await product.Update();

            }
            catch
            {
                return Conflict();
            }

            return Ok();
        }
    }

    public static class DiagnosticsAreReturned_ForControllerWithCustomConvention
    {
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public static void Update(int id, Product product)
        {

        }
    }

    public class Product
    {
        public Task Update() => Task.CompletedTask;
    }
}
