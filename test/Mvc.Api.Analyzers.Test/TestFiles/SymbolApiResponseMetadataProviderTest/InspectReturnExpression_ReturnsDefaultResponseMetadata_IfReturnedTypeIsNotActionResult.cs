namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResult : ControllerBase
    {
        public object Get()
        {
            return new InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResultModel();
        }
    }

    public class InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResultModel { }
}
