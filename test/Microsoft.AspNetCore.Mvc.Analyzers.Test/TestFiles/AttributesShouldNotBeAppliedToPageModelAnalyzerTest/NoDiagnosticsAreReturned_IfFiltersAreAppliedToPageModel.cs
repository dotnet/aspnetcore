using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [ServiceFilter(typeof(object))]
    public class NoDiagnosticsAreReturned_IfFiltersAreAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
