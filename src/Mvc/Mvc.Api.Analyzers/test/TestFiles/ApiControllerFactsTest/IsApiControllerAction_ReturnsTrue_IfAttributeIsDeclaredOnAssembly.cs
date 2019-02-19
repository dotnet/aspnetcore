using Microsoft.AspNetCore.Mvc;

[assembly: ApiController]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiControllerFactsTest
{
    public class IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssemblyController : ControllerBase
    {
        public IActionResult Action() => null;
    }
}
