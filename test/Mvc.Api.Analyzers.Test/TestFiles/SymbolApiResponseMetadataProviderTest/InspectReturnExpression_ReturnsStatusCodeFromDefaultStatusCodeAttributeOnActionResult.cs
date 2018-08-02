namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult : ControllerBase
    {
        public IActionResult Get()
        {
            return Unauthorized();
        }
    }
}
