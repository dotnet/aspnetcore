using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    public class Home : PageModel
    {
        public IActionResult OnPost(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            return Page();
        }
    }
}
