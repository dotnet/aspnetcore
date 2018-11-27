using Microsoft.AspNetCore.Mvc;

[assembly: ProducesErrorResponseType(typeof(Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest.GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.SymbolApiResponseMetadataProviderTest
{
    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyController
    {
        public void Action() { }
    }

    public class GetErrorResponseType_ReturnsTypeDefinedAtAssemblyModel { }
}
