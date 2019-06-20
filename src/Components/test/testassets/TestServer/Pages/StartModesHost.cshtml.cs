using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestServer
{
    public class StartModesHostModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        [FromRoute]
        public string Mode { get; set; }

        public void OnGet()
        {
        }
    }
}
