using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite
{
    public class RedirectFromModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("/Pages/Redirects/Redirect", new { id = 12});
    }
}
