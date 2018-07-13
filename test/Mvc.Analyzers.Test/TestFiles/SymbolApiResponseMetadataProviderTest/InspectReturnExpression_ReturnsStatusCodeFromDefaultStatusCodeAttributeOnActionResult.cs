namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult : ControllerBase
    {
        public IActionResult Get()
        {
            return Unauthorized();
        }
    }
}
