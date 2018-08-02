using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class Home : PageModel
    {
        [ProducesResponseType(302)]
        public IActionResult OnPost(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
