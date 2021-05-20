using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    [PageModel]
    public abstract class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
        [/*MM*/Authorize]
        public void OnGet() { }
    }

    public class DiagnosticsAreReturned_IfAttributeIsAppliedToBaseType : DiagnosticsAreReturned_IfAttributeIsAppliedToBaseTypeBase
    {
    }
}
