using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [/*MM*/Route("/mypage")]
    public class DiagnosticsAreReturned_IfRouteAttribute_IsAppliedToPageModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
