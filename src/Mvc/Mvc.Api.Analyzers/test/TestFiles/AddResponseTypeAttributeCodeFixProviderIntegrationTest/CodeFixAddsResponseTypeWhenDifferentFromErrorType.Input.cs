namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ProducesErrorResponseType(typeof(CodeFixAddsResponseTypeWhenDifferentErrorModel))]
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixAddsResponseTypeWhenDifferentFromErrorType : ControllerBase
    {
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
