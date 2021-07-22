using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ProducesErrorResponseType(typeof(CodeFixAddsResponseTypeWhenDifferentErrorModel))]
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsResponseTypeWhenDifferentFromErrorType : ControllerBase
    {
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public IActionResult GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound(new CodeFixAddsResponseTypeWhenDifferentErrorModel());
            }

            if (id == 1)
            {
                var validationProblemDetails = new ValidationProblemDetails(ModelState);
                return BadRequest(validationProblemDetails);
            }

            return Ok(new object());
        }
    }

    public class CodeFixAddsResponseTypeWhenDifferentErrorModel { }
}
