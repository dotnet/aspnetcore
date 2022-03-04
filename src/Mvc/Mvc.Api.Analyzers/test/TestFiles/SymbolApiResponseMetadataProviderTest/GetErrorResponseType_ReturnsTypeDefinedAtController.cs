namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    [ProducesErrorResponseType(typeof(GetErrorResponseType_ReturnsTypeDefinedAtControllerModel))]
    public class GetErrorResponseType_ReturnsTypeDefinedAtControllerController
    {
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtControllerModel { }
}
