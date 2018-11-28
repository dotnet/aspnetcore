using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class DiagnosticsAreReturned_IfRouteAttributesAreAppliedToPageHandlerMethod : PageModel
    {
        [/*MM*/HttpHead]
        public void OnGet()
        {
        }
    }
}
