using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [Authorize]
    public class NoDiagnosticsAreReturned_IfAuthorizeAttributeIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
