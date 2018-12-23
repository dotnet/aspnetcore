using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesApp
{
    public class MyPageModel : PageModel
    {
        public string Name { get; private set; }

        public IActionResult OnGet(string person)
        {
            Name = person;
            return Page();
        }
    }
}
