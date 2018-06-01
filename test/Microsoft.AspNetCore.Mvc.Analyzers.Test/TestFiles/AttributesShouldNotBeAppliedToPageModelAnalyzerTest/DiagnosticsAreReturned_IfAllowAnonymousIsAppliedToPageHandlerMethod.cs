using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfAllowAnonymousIsAppliedToPageHandlerMethod : PageModel
    {
        [/*MM*/AllowAnonymous]
        public void OnGet()
        {

        }

        public void OnPost()
        {
        }
    }
}
