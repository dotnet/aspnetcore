using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Analyzers.Test
{
    public class NoDiagnosticsAreReturned_ForPageHandlersWithNonFilterAttributes : PageModel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnGet()
        {
        }
    }
}
