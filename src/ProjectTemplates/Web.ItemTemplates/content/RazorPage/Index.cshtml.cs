using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyApp.Namespace
{
#if NameIsPage
    public class IndexModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
#else
    public class IndexModel : PageModel
#endif
    {
        public void OnGet()
        {
        }
    }
}
