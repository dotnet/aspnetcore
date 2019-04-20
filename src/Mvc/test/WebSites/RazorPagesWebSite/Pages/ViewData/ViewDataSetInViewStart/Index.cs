using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RazorPagesWebSite.ViewDataSetInViewStart
{
    public class Index : PageModel
    {
        [ViewData]
        public string ValueFromPageModel => "Value from Page Model";
    }
}
