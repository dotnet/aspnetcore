using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageHandlerMethod : PageModel
    {
        [/*MM*/Authorize]
        public void OnPost()
        {
        }
    }
}
