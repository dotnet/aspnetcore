using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [AllowAnonymous]
    public class NoDiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
