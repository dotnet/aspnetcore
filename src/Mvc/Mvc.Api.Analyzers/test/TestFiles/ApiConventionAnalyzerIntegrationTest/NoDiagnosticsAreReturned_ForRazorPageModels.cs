using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public class Home : PageModel
    {
#pragma warning disable MVC1001
        [ProducesResponseType(302)]
        public IActionResult OnPost(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Page();
        }
#pragma warning restore MVC1001
    }
}
