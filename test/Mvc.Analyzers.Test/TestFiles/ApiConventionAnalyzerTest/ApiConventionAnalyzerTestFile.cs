using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class UnwrapMethodReturnType
    {
        public ApiConventionAnalyzerBaseModel ReturnsBaseModel() => null;

        public ActionResult<ApiConventionAnalyzerBaseModel> ReturnsActionResultOfBaseModel() => null;

        public Task<ActionResult<ApiConventionAnalyzerBaseModel>> ReturnsTaskOfActionResultOfBaseModel() => null;

        public ValueTask<ActionResult<ApiConventionAnalyzerBaseModel>> ReturnsValueTaskOfActionResultOfBaseModel() => default(ValueTask<ActionResult<ApiConventionAnalyzerBaseModel>>);

        public ActionResult<IEnumerable<ApiConventionAnalyzerBaseModel>> ReturnsActionResultOfIEnumerableOfBaseModel() => null;

        public IEnumerable<ApiConventionAnalyzerBaseModel> ReturnsIEnumerableOfBaseModel() => null;
    }

    [DefaultStatusCode(StatusCodes.Status412PreconditionFailed)]
    public class TestActionResultUsingStatusCodesConstants { }

    [DefaultStatusCode((int)HttpStatusCode.Found)]
    public class TestActionResultUsingHttpStatusCodeCast { }

    public class ApiConventionAnalyzerBaseModel { }

    public class ApiConventionAnalyzerDerivedModel : ApiConventionAnalyzerBaseModel { }

    public class ApiConventionAnalyzerTest_IndexModel : PageModel
    {
        public IActionResult OnGet() => null;
    }

    public class ApiConventionAnalyzerTest_NotApiController : Controller
    {
        public IActionResult Index() => null;
    }

    public class ApiConventionAnalyzerTest_NotAction : Controller
    {
        [NonAction]
        public IActionResult Index() => null;
    }

    [ApiController]
    public class ApiConventionAnalyzerTest_Valid : Controller
    {
        public IActionResult Index() => null;
    }
}
